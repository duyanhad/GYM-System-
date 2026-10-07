using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Quyền chi tiết (RBAC). Danh sách mã quyền nằm trong Authorization/PermissionConstants.cs.
/// </summary>
public partial class Permission
{
    public Guid PermissionId { get; set; }

    public string PermissionCode { get; set; } = null!;

    public string PermissionName { get; set; } = null!;

    /// <summary>Nhóm quyền để hiển thị UI (MEMBER, CLASS, BILLING, ...).</summary>
    public string? GroupName { get; set; }

    /// <summary>Alias tiện dụng cho code controller.</summary>
    public string Code => PermissionCode;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
