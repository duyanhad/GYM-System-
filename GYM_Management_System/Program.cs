using System;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.Hubs;
using GYM_Management_System.Middleware;
using GYM_Management_System.Models;
using GYM_Management_System.Seed;
using GYM_Management_System.Services;
using GYM_Management_System.Services.AdminServices;
using GYM_Management_System.Services.AttendanceServices;
using GYM_Management_System.Services.AuthServices;
using GYM_Management_System.Services.BillingServices;
using GYM_Management_System.Services.BranchServices;
using GYM_Management_System.Services.ClassServices;
using GYM_Management_System.Services.DashboardServices;
using GYM_Management_System.Services.Email;
using GYM_Management_System.Services.MemberServices;
using GYM_Management_System.Services.MembershipServices;
using GYM_Management_System.Services.SystemServices;
using GYM_Management_System.Services.TrainerServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// DATABASE
// - PostgreSQL: môi trường thật / deploy (dùng EF Core Migrations)
// - SQLite: môi trường dev trên máy cá nhân (tự tạo schema bằng EnsureCreated)
// Đổi provider trong appsettings.json (DatabaseProvider) hoặc appsettings.Development.json.
// =========================================================================
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "PostgreSQL";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Thiếu cấu hình ConnectionStrings:DefaultConnection.");

var useSqlite = databaseProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<GymDbContext>(options =>
{
    if (useSqlite)
        options.UseSqlite(connectionString);
    else
        options.UseNpgsql(connectionString);
});

// =========================================================================
// DEPENDENCY INJECTION - SERVICES
// =========================================================================
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<INotificationSender, SignalRNotificationSender>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

// Xác thực & phân quyền
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminRoleService, AdminRoleService>();

// Hội viên & gói tập
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IMembershipPlanService, MembershipPlanService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Huấn luyện viên & lớp học
builder.Services.AddScoped<ITrainerService, TrainerService>();
builder.Services.AddScoped<ITrainingClassService, TrainingClassService>();
builder.Services.AddScoped<IClassScheduleService, ClassScheduleService>();
builder.Services.AddScoped<IClassBookingService, ClassBookingService>();

// Ra vào & tài chính
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Chi nhánh, dashboard & hệ thống
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<ISystemNotificationService, SystemNotificationService>();

// =========================================================================
// CONTROLLERS
// =========================================================================
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Chuẩn hóa message lỗi validate model sang tiếng Việt theo format ApiResponse
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(x =>
                {
                    var fieldName = e.Key.Split('.').Last();
                    var msg = x.ErrorMessage;

                    if (string.IsNullOrEmpty(msg) && x.Exception != null)
                        msg = x.Exception.Message;

                    if (string.IsNullOrEmpty(msg))
                        msg = $"Trường '{fieldName}' không hợp lệ.";
                    else if (msg.Contains("is required"))
                        msg = $"Trường '{fieldName}' là bắt buộc.";
                    else if (msg.Contains("Could not convert") || msg.Contains("could not convert") || msg.Contains("The JSON value"))
                        msg = $"Trường '{fieldName}' không đúng định dạng.";

                    return msg;
                }))
                .ToArray();

            return new BadRequestObjectResult(new
            {
                success = false,
                message = "Dữ liệu không hợp lệ.",
                errors = errors.Length > 0 ? errors : new[] { "Dữ liệu gửi lên không đúng định dạng." }
            });
        };
    });

// =========================================================================
// RATE LIMITING (chống spam login / OTP)
// =========================================================================
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("StrictLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// =========================================================================
// AUTHENTICATION (JWT Bearer)
// =========================================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        // SignalR client gửi token qua query string ?access_token=
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/r/gymHub"))
                context.Token = accessToken;

            return Task.CompletedTask;
        }
    };
});

// =========================================================================
// RBAC: đăng ký Authorization Handler + toàn bộ Policy Permission
// =========================================================================
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options => PermissionPolicyFactory.RegisterAll(options));

// =========================================================================
// REALTIME + CORS
// =========================================================================
builder.Services.AddSignalR();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:3000", "http://localhost:5173", "http://localhost:8081" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// =========================================================================
// SWAGGER
// =========================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GYM Management System API",
        Version = "v1",
        Description = "API quản lý phòng gym: hội viên, gói tập, lớp học, check-in, hóa đơn, phân quyền."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token nhận được từ /api/auth/login"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// =========================================================================
// PIPELINE
// =========================================================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GYM Management System API v1");
    c.DocumentTitle = "GYM Management System API";
});

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseGlobalExceptionHandler(); // Bắt tất cả exception, trả về ApiResponse chuẩn
app.UseActiveUserCheck();        // Chặn tài khoản đã bị khóa/tạm ngưng ngay khi token còn hạn
app.UseAuthorization();

app.MapControllers();
app.MapHub<GymHub>("/r/gymHub");

// =========================================================================
// KHỞI TẠO DATABASE + SEED DỮ LIỆU MẪU
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();

    if (useSqlite)
    {
        // SQLite dùng cho dev nhanh: tạo schema trực tiếp (không dùng migration của PostgreSQL)
        await context.Database.EnsureCreatedAsync();
    }
    else
    {
        await context.Database.MigrateAsync();
    }

    await DatabaseSeeder.SeedAsync(context);
}

app.Run();
