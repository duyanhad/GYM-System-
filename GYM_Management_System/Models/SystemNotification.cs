using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Thông báo trong hệ thống (gửi cho 1 user cụ thể, 1 chi nhánh hoặc toàn hệ thống).
/// </summary>
public partial class SystemNotification
{
    public Guid NotificationId { get; set; }

    public string Title { get; set; } = "";

    public string Message { get; set; } = "";

    /// <summary>INFO / WARNING / SUCCESS / ERROR.</summary>
    public string Type { get; set; } = "INFO";

    /// <summary>Người nhận (null = thông báo toàn hệ thống).</summary>
    public Guid? UserId { get; set; }

    public Guid? BranchId { get; set; }

    /// <summary>Loại đối tượng liên quan: MEMBER, SUBSCRIPTION, INVOICE, ...</summary>
    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
