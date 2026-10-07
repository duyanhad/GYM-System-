using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Nhật ký thao tác quan trọng trong hệ thống (audit log).
/// </summary>
public partial class SystemLog
{
    public Guid LogId { get; set; }

    public Guid? UserId { get; set; }

    public string? Username { get; set; }

    /// <summary>Hành động: LOGIN, CREATE_MEMBER, UPDATE_SUBSCRIPTION, ...</summary>
    public string Action { get; set; } = "";

    public string? EntityName { get; set; }

    public Guid? EntityId { get; set; }

    public string? Description { get; set; }

    /// <summary>INFO / WARNING / ERROR (xem <see cref="DomainConstants.LogLevel"/>).</summary>
    public string Level { get; set; } = DomainConstants.LogLevel.Info;

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }
}
