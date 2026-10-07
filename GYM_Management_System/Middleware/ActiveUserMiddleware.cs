using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Middleware;

/// <summary>
/// Kiểm tra tài khoản còn hiệu lực tại mỗi request.
/// Token có thời hạn dài nên khi Admin khóa/xóa tài khoản, request tiếp theo phải bị chặn ngay
/// thay vì phải chờ token hết hạn.
/// </summary>
public class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, GymDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .Select(u => new { u.UserId, u.IsActive, u.Status, u.SuspendedUntil })
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    await WriteErrorAsync(context, 401, "Tài khoản không tồn tại. Vui lòng đăng nhập lại.");
                    return;
                }

                var isBlocked = !user.IsActive
                    || user.Status == DomainConstants.UserStatus.Deleted
                    || user.Status == DomainConstants.UserStatus.Banned
                    || (user.Status == DomainConstants.UserStatus.Suspended
                        && (user.SuspendedUntil == null || user.SuspendedUntil > DateTime.UtcNow));

                if (isBlocked)
                {
                    await WriteErrorAsync(context, 403, "Tài khoản của bạn đã bị tạm ngưng hoặc khóa. Vui lòng liên hệ quản trị viên.");
                    return;
                }
            }
        }

        await _next(context);
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Errors = new() { message }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

public static class ActiveUserMiddlewareExtensions
{
    public static IApplicationBuilder UseActiveUserCheck(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ActiveUserMiddleware>();
    }
}
