using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GYM_Management_System.Models;

/// <summary>
/// DbContext trung tâm của hệ thống quản lý phòng gym.
/// Toàn bộ cấu hình bảng, khóa, index và quan hệ được khai báo tập trung tại đây.
/// </summary>
public partial class GymDbContext : DbContext
{
    public GymDbContext()
    {
    }

    public GymDbContext(DbContextOptions<GymDbContext> options)
        : base(options)
    {
    }

    // ===== Người dùng & phân quyền =====
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<Permission> Permissions { get; set; }
    public virtual DbSet<UserPermission> UserPermissions { get; set; }
    public virtual DbSet<OtpVerification> OtpVerifications { get; set; }
    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    // ===== Chi nhánh =====
    public virtual DbSet<Branch> Branches { get; set; }

    // ===== Hội viên & gói tập =====
    public virtual DbSet<Member> Members { get; set; }
    public virtual DbSet<MembershipPlan> MembershipPlans { get; set; }
    public virtual DbSet<MemberSubscription> MemberSubscriptions { get; set; }

    // ===== Huấn luyện viên & lớp học =====
    public virtual DbSet<Trainer> Trainers { get; set; }
    public virtual DbSet<TrainingClass> TrainingClasses { get; set; }
    public virtual DbSet<ClassSchedule> ClassSchedules { get; set; }
    public virtual DbSet<ClassBooking> ClassBookings { get; set; }

    // ===== Ra vào & tài chính =====
    public virtual DbSet<CheckIn> CheckIns { get; set; }
    public virtual DbSet<Invoice> Invoices { get; set; }
    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }

    // ===== Hệ thống =====
    public virtual DbSet<SystemLog> SystemLogs { get; set; }
    public virtual DbSet<SystemNotification> SystemNotifications { get; set; }

    // ===== Tập luyện: bài tập, giáo án, lịch tuần, buổi tập =====
    public virtual DbSet<Exercise> Exercises { get; set; }
    public virtual DbSet<WorkoutPlan> WorkoutPlans { get; set; }
    public virtual DbSet<WorkoutPlanItem> WorkoutPlanItems { get; set; }
    public virtual DbSet<WeeklyScheduleEntry> WeeklyScheduleEntries { get; set; }
    public virtual DbSet<WorkoutSession> WorkoutSessions { get; set; }
    public virtual DbSet<WorkoutSessionExercise> WorkoutSessionExercises { get; set; }
    public virtual DbSet<WorkoutSetLog> WorkoutSetLogs { get; set; }
    public virtual DbSet<WorkoutPlanShare> WorkoutPlanShares { get; set; }
    public virtual DbSet<UserConnection> UserConnections { get; set; }
    public virtual DbSet<UserNotification> UserNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =====================================================================
        // NGƯỜI DÙNG & PHÂN QUYỀN
        // =====================================================================
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email);

            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(30);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(30);

            entity.HasOne(e => e.DefaultBranch)
                .WithMany()
                .HasForeignKey(e => e.DefaultBranchId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Roles)
                .WithMany(r => r.Users)
                .UsingEntity(join => join.ToTable("UserRoles"));
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.RoleId);
            entity.HasIndex(e => e.RoleName).IsUnique();
            entity.Property(e => e.RoleName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(300);

            entity.HasMany(e => e.Permissions)
                .WithMany(p => p.Roles)
                .UsingEntity(join => join.ToTable("RolePermissions"));
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(e => e.PermissionId);
            entity.HasIndex(e => e.PermissionCode).IsUnique();
            entity.Property(e => e.PermissionCode).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PermissionName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.GroupName).HasMaxLength(100);
            entity.Ignore(e => e.Code);
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("UserPermissions");
            entity.HasKey(e => e.UserPermissionId);
            entity.HasIndex(e => new { e.UserId, e.PermissionId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OtpVerification>(entity =>
        {
            entity.ToTable("OtpVerifications");
            entity.HasKey(e => e.OtpId);
            entity.HasIndex(e => new { e.Email, e.OtpCode });
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.OtpCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Purpose).HasMaxLength(50);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens");
            entity.HasKey(e => e.TokenId);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).HasMaxLength(200).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================================
        // CHI NHÁNH
        // =====================================================================
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branches");
            entity.HasKey(e => e.BranchId);
            entity.HasIndex(e => e.BranchCode).IsUnique();
            entity.Property(e => e.BranchName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.BranchCode).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.ManagerUser)
                .WithMany()
                .HasForeignKey(e => e.ManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =====================================================================
        // HỘI VIÊN & GÓI TẬP
        // =====================================================================
        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("Members");
            entity.HasKey(e => e.MemberId);
            entity.HasIndex(e => e.MemberCode).IsUnique();
            entity.HasIndex(e => e.Phone);

            entity.Property(e => e.MemberCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(200);
            entity.Property(e => e.EmergencyContactPhone).HasMaxLength(30);
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Branch)
                .WithMany(b => b.Members)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MembershipPlan>(entity =>
        {
            entity.ToTable("MembershipPlans");
            entity.HasKey(e => e.PlanId);
            entity.HasIndex(e => e.PlanCode).IsUnique();
            entity.Property(e => e.PlanCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PlanName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasPrecision(18, 2);

            entity.HasOne(e => e.Branch)
                .WithMany(b => b.MembershipPlans)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MemberSubscription>(entity =>
        {
            entity.ToTable("MemberSubscriptions");
            entity.HasKey(e => e.SubscriptionId);
            entity.HasIndex(e => e.SubscriptionCode).IsUnique();
            entity.HasIndex(e => new { e.MemberId, e.Status });

            entity.Property(e => e.SubscriptionCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.PaymentStatus).HasMaxLength(30).IsRequired();
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.FinalAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Member)
                .WithMany(m => m.Subscriptions)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Branch)
                .WithMany(b => b.Subscriptions)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =====================================================================
        // HUẤN LUYỆN VIÊN & LỚP HỌC
        // =====================================================================
        modelBuilder.Entity<Trainer>(entity =>
        {
            entity.ToTable("Trainers");
            entity.HasKey(e => e.TrainerId);
            entity.HasIndex(e => e.TrainerCode).IsUnique();
            entity.Property(e => e.TrainerCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Specialization).HasMaxLength(200);
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.HourlyRate).HasPrecision(18, 2);

            entity.HasOne(e => e.Branch)
                .WithMany(b => b.Trainers)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TrainingClass>(entity =>
        {
            entity.ToTable("TrainingClasses");
            entity.HasKey(e => e.ClassId);
            entity.HasIndex(e => e.ClassCode).IsUnique();
            entity.Property(e => e.ClassCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ClassName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Level).HasMaxLength(30);
            entity.Property(e => e.PricePerSession).HasPrecision(18, 2);

            entity.HasOne(e => e.Trainer)
                .WithMany(t => t.TrainingClasses)
                .HasForeignKey(e => e.TrainerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Branch)
                .WithMany(b => b.TrainingClasses)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ClassSchedule>(entity =>
        {
            entity.ToTable("ClassSchedules");
            entity.HasKey(e => e.ScheduleId);
            entity.Property(e => e.Room).HasMaxLength(100);

            entity.HasOne(e => e.TrainingClass)
                .WithMany(c => c.Schedules)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Trainer)
                .WithMany(t => t.ClassSchedules)
                .HasForeignKey(e => e.TrainerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ClassBooking>(entity =>
        {
            entity.ToTable("ClassBookings");
            entity.HasKey(e => e.BookingId);
            entity.HasIndex(e => new { e.MemberId, e.ClassId });
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.TrainingClass)
                .WithMany(c => c.Bookings)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Schedule)
                .WithMany(s => s.Bookings)
                .HasForeignKey(e => e.ScheduleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Member)
                .WithMany(m => m.ClassBookings)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================================
        // RA VÀO & TÀI CHÍNH
        // =====================================================================
        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.ToTable("CheckIns");
            entity.HasKey(e => e.CheckInId);
            entity.HasIndex(e => new { e.MemberId, e.CheckInTime });
            entity.Property(e => e.Method).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Member)
                .WithMany(m => m.CheckIns)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Branch)
                .WithMany(b => b.CheckIns)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(e => e.InvoiceId);
            entity.HasIndex(e => e.InvoiceCode).IsUnique();
            entity.Property(e => e.InvoiceCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Member)
                .WithMany(m => m.Invoices)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Branch)
                .WithMany(b => b.Invoices)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Subscription)
                .WithMany(s => s.Invoices)
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.ToTable("InvoiceItems");
            entity.HasKey(e => e.InvoiceItemId);
            entity.Property(e => e.ItemType).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ItemName).HasMaxLength(300).IsRequired();
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(e => e.PaymentId);
            entity.HasIndex(e => e.PaymentCode).IsUnique();
            entity.Property(e => e.PaymentCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PaymentMethod).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Reference).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Member)
                .WithMany(m => m.Payments)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Branch)
                .WithMany(b => b.Payments)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =====================================================================
        // HỆ THỐNG
        // =====================================================================
        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.ToTable("SystemLogs");
            entity.HasKey(e => e.LogId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Action).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Level).HasMaxLength(20);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
        });

        modelBuilder.Entity<SystemNotification>(entity =>
        {
            entity.ToTable("SystemNotifications");
            entity.HasKey(e => e.NotificationId);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(30);
            entity.Property(e => e.ReferenceType).HasMaxLength(50);
        });

        // =====================================================================
        // TẬP LUYỆN: BÀI TẬP / GIÁO ÁN / LỊCH TUẦN / BUỔI TẬP
        // =====================================================================
        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.ToTable("Exercises");
            entity.HasKey(e => e.ExerciseId);
            entity.HasIndex(e => new { e.UserId, e.Name });
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MuscleGroup).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(500);
        });

        modelBuilder.Entity<WorkoutPlan>(entity =>
        {
            entity.ToTable("WorkoutPlans");
            entity.HasKey(e => e.WorkoutPlanId);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Focus).HasMaxLength(200);
            entity.Property(e => e.Note).HasMaxLength(1000);
        });

        modelBuilder.Entity<WorkoutPlanItem>(entity =>
        {
            entity.ToTable("WorkoutPlanItems");
            entity.HasKey(e => e.WorkoutPlanItemId);
            entity.HasIndex(e => new { e.WorkoutPlanId, e.OrderIndex });
            entity.Property(e => e.ExerciseName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasOne(e => e.WorkoutPlan)
                .WithMany(p => p.Items)
                .HasForeignKey(e => e.WorkoutPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WeeklyScheduleEntry>(entity =>
        {
            entity.ToTable("WeeklyScheduleEntries");
            entity.HasKey(e => e.WeeklyScheduleEntryId);
            entity.HasIndex(e => new { e.UserId, e.DayOfWeek }).IsUnique();

            entity.HasOne<WorkoutPlan>()
                .WithMany()
                .HasForeignKey(e => e.WorkoutPlanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.ToTable("WorkoutSessions");
            entity.HasKey(e => e.WorkoutSessionId);
            entity.HasIndex(e => new { e.UserId, e.SessionDate });
            entity.Property(e => e.PlanName).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(1000);

            entity.HasOne<WorkoutPlan>()
                .WithMany()
                .HasForeignKey(e => e.WorkoutPlanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkoutSessionExercise>(entity =>
        {
            entity.ToTable("WorkoutSessionExercises");
            entity.HasKey(e => e.WorkoutSessionExerciseId);
            entity.HasIndex(e => new { e.WorkoutSessionId, e.OrderIndex });
            entity.Property(e => e.ExerciseName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MuscleGroup).HasMaxLength(50);
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasOne(e => e.WorkoutSession)
                .WithMany(s => s.Exercises)
                .HasForeignKey(e => e.WorkoutSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutSetLog>(entity =>
        {
            entity.ToTable("WorkoutSetLogs");
            entity.HasKey(e => e.WorkoutSetLogId);
            entity.HasIndex(e => new { e.WorkoutSessionExerciseId, e.SetNumber });
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();

            entity.HasOne(e => e.SessionExercise)
                .WithMany(x => x.Sets)
                .HasForeignKey(e => e.WorkoutSessionExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutPlanShare>(entity =>
        {
            entity.ToTable("WorkoutPlanShares");
            entity.HasKey(e => e.WorkoutPlanShareId);
            entity.HasIndex(e => new { e.ToUserId, e.WorkoutPlanId });
            entity.Property(e => e.Message).HasMaxLength(500);

            entity.HasOne<WorkoutPlan>()
                .WithMany()
                .HasForeignKey(e => e.WorkoutPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserConnection>(entity =>
        {
            entity.ToTable("UserConnections");
            entity.HasKey(e => e.UserConnectionId);
            entity.HasIndex(e => new { e.RequesterUserId, e.AddresseeUserId }).IsUnique();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.ToTable("UserNotifications");
            entity.HasKey(e => e.UserNotificationId);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        // =====================================================================
        // SQLite (chỉ dùng cho môi trường dev) không hỗ trợ SUM/AVG trên kiểu decimal,
        // nên khi chạy SQLite ta lưu decimal dưới dạng double. PostgreSQL giữ nguyên decimal(18,2).
        // =====================================================================
        if (Database.IsSqlite())
        {
            var decimalConverter = new ValueConverter<decimal, double>(
                value => (double)value,
                value => (decimal)value);

            var decimalProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(entityType => entityType.GetProperties())
                .Where(property => property.ClrType == typeof(decimal));

            foreach (var property in decimalProperties)
                property.SetValueConverter(decimalConverter);
        }
    }
}
