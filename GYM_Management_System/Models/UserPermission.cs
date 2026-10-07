using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Ghi đè quyền cho từng user cụ thể.
/// <c>null</c> = kế thừa theo Role, <c>true</c> = cấp thêm, <c>false</c> = chặn (deny).
/// </summary>
public partial class UserPermission
{
    public Guid UserPermissionId { get; set; }

    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    public bool? IsGranted { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
