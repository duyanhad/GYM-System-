using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

public partial class Role
{
    public Guid RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Role hệ thống thì không cho phép xóa qua API.</summary>
    public bool IsSystem { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
