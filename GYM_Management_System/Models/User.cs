using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Tài khoản đăng nhập hệ thống (Admin / Chủ phòng gym / Lễ tân / PT).
/// </summary>
public partial class User
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsVerified { get; set; }

    /// <summary>ACTIVE / SUSPENDED / BANNED / DELETED (xem <see cref="DomainConstants.UserStatus"/>).</summary>
    public string? Status { get; set; }

    public Guid? DefaultBranchId { get; set; }

    /// <summary>Người tạo tài khoản này (Admin hoặc chủ phòng gym).</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime? SuspendedUntil { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branch? DefaultBranch { get; set; }

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
