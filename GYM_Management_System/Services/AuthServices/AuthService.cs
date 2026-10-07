using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs.AuthDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using GYM_Management_System.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.AuthServices;

public class AuthService : IAuthService
{
    private const int OtpValidMinutes = 10;

    private readonly GymDbContext _context;
    private readonly JwtHelper _jwtHelper;
    private readonly IEmailService _emailService;

    public AuthService(GymDbContext context, JwtHelper jwtHelper, IEmailService emailService)
    {
        _context = context;
        _jwtHelper = jwtHelper;
        _emailService = emailService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            throw new BusinessException("Tên đăng nhập và mật khẩu là bắt buộc.");

        var username = dto.Username.Trim().ToLower();

        var user = await _context.Users
            .Include(u => u.Roles).ThenInclude(r => r.Permissions)
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .Include(u => u.DefaultBranch)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username || u.Email.ToLower() == username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new BusinessException("Tên đăng nhập hoặc mật khẩu không đúng.");

        if (!user.IsActive || user.Status == DomainConstants.UserStatus.Deleted || user.Status == DomainConstants.UserStatus.Banned)
            throw new BusinessException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

        if (user.Status == DomainConstants.UserStatus.Suspended
            && (user.SuspendedUntil == null || user.SuspendedUntil > DateTime.UtcNow))
            throw new BusinessException("Tài khoản đang bị tạm ngưng. Vui lòng liên hệ quản trị viên.");

        var roles = user.Roles.Select(r => r.RoleName).ToList();
        var permissions = await ResolvePermissionsAsync(user, roles);

        var token = _jwtHelper.GenerateToken(
            user.UserId,
            user.Username,
            user.FullName,
            roles,
            permissions,
            user.DefaultBranchId);

        user.Status ??= DomainConstants.UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;

        _context.SystemLogs.Add(new SystemLog
        {
            LogId = Guid.NewGuid(),
            UserId = user.UserId,
            Username = user.Username,
            Action = "LOGIN",
            EntityName = nameof(User),
            EntityId = user.UserId,
            Description = $"Người dùng {user.Username} đăng nhập hệ thống.",
            Level = DomainConstants.LogLevel.Info,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = _jwtHelper.GetExpiryTime(),
            User = MapUser(user, roles, permissions)
        };
    }

    public async Task<UserResponseDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Roles).ThenInclude(r => r.Permissions)
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .Include(u => u.DefaultBranch)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        var roles = user.Roles.Select(r => r.RoleName).ToList();
        var permissions = await ResolvePermissionsAsync(user, roles);

        return MapUser(user, roles, permissions);
    }

    public async Task<UserResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Roles).ThenInclude(r => r.Permissions)
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .Include(u => u.DefaultBranch)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName.Trim();

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber.Trim();

        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl.Trim();

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();
            var emailTaken = await _context.Users.AnyAsync(u => u.UserId != userId && u.Email.ToLower() == email.ToLower());
            if (emailTaken) throw new BusinessException("Email đã được sử dụng bởi tài khoản khác.");

            user.Email = email;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var roles = user.Roles.Select(r => r.RoleName).ToList();
        var permissions = await ResolvePermissionsAsync(user, roles);

        return MapUser(user, roles, permissions);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new BusinessException("Mật khẩu xác nhận không khớp.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BusinessException("Mật khẩu hiện tại không đúng.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task SendForgotPasswordOtpAsync(ForgotPasswordStartDto dto)
    {
        var email = dto.Email.Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        // Không tiết lộ email có tồn tại hay không
        if (user == null) return;

        var otpCode = CodeGenerator.OtpCode();

        _context.OtpVerifications.Add(new OtpVerification
        {
            OtpId = Guid.NewGuid(),
            Email = email,
            OtpCode = otpCode,
            Purpose = "FORGOT_PASSWORD",
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpValidMinutes),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await _emailService.SendOtpAsync(email, otpCode, "FORGOT_PASSWORD");
    }

    public async Task ResetPasswordWithOtpAsync(ForgotPasswordVerifyDto dto)
    {
        var email = dto.Email.Trim();

        var otp = await _context.OtpVerifications
            .Where(o => o.Email.ToLower() == email.ToLower()
                        && o.OtpCode == dto.OtpCode
                        && o.Purpose == "FORGOT_PASSWORD"
                        && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw new BusinessException("Mã OTP không hợp lệ hoặc đã được sử dụng.");

        if (otp.ExpiresAt < DateTime.UtcNow)
            throw new BusinessException("Mã OTP đã hết hiệu lực. Vui lòng yêu cầu mã mới.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower())
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản với email này.");

        otp.IsUsed = true;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Tổng hợp quyền hiệu lực của user: quyền theo role + quyền cấp riêng, trừ quyền bị chặn.
    /// Admin/Manager mặc định có toàn bộ quyền (Manager không có nhóm ADMIN_).
    /// </summary>
    private async Task<List<string>> ResolvePermissionsAsync(User user, List<string> roles)
    {
        var allCodes = await _context.Permissions.AsNoTracking().Select(p => p.PermissionCode).ToListAsync();

        var isAdmin = roles.Any(r => string.Equals(r, DomainConstants.AdminRole, StringComparison.OrdinalIgnoreCase));
        if (isAdmin) return allCodes;

        var isManager = roles.Any(r => string.Equals(r, DomainConstants.ManagerRole, StringComparison.OrdinalIgnoreCase));

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (isManager)
        {
            foreach (var code in allCodes.Where(c => !c.StartsWith("ADMIN_")))
                permissions.Add(code);
        }
        else
        {
            foreach (var code in user.Roles.SelectMany(r => r.Permissions).Select(p => p.PermissionCode))
                permissions.Add(code);
        }

        foreach (var overrideEntry in user.UserPermissions)
        {
            var code = overrideEntry.Permission?.PermissionCode;
            if (string.IsNullOrEmpty(code)) continue;

            if (overrideEntry.IsGranted == false)
                permissions.Remove(code);
            else
                permissions.Add(code);
        }

        return permissions.OrderBy(p => p).ToList();
    }

    private static UserResponseDto MapUser(User user, List<string> roles, List<string> permissions) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        Status = user.Status,
        IsActive = user.IsActive,
        DefaultBranchId = user.DefaultBranchId,
        DefaultBranchName = user.DefaultBranch?.BranchName,
        Roles = roles,
        Permissions = permissions
    };
}
