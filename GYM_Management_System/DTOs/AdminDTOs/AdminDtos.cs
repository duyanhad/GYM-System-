using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GYM_Management_System.DTOs.AdminDTOs;

public class SystemUserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public string? Status { get; set; }
    public Guid? DefaultBranchId { get; set; }
    public string? DefaultBranchName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class CreateUserRequest
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
    [MaxLength(100)]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    [MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = "";

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }
    public Guid? DefaultBranchId { get; set; }

    /// <summary>Danh sách tên role gán cho user (vd: Manager, Receptionist, Trainer).</summary>
    public List<string> Roles { get; set; } = new();
}

public class UpdateUserRequest
{
    [MaxLength(200)]
    public string? FullName { get; set; }

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }
    public Guid? DefaultBranchId { get; set; }
    public List<string>? Roles { get; set; }
}

public class ChangeUserStatusRequest
{
    /// <summary>ACTIVE / SUSPENDED / BANNED.</summary>
    [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
    public string Status { get; set; } = "";

    public DateTime? SuspendedUntil { get; set; }
}

public class AdminResetPasswordRequest
{
    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    public string NewPassword { get; set; } = "";
}

public class UserQueryParameters
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public Guid? BranchId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class RoleDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class CreateRoleRequest
{
    [Required(ErrorMessage = "Tên role là bắt buộc.")]
    [MaxLength(100)]
    public string RoleName { get; set; } = "";

    [MaxLength(300)]
    public string? Description { get; set; }

    public List<string> Permissions { get; set; } = new();
}

public class UpdateRoleRequest
{
    [MaxLength(300)]
    public string? Description { get; set; }
    public List<string>? Permissions { get; set; }
}

public class PermissionDto
{
    public Guid PermissionId { get; set; }
    public string PermissionCode { get; set; } = "";
    public string PermissionName { get; set; } = "";
    public string? GroupName { get; set; }
}

public class AssignUserPermissionsRequest
{
    /// <summary>
    /// Danh sách quyền ghi đè cho user.
    /// IsGranted = true: cấp thêm, false: chặn, null: kế thừa theo role.
    /// </summary>
    public List<UserPermissionOverrideDto> Overrides { get; set; } = new();
}

public class UserPermissionOverrideDto
{
    [Required]
    public string PermissionCode { get; set; } = "";

    public bool? IsGranted { get; set; }
}

public class IdResponseDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
}
