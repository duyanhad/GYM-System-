using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Thông báo đơn giản gửi cho một tài khoản (VD: được chia sẻ giáo án).
/// </summary>
public partial class UserNotification
{
    public Guid UserNotificationId { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = "";

    public string? Message { get; set; }

    /// <summary>PLAN_SHARED / CONNECTION_REQUEST / CONNECTION_ACCEPTED.</summary>
    public string Type { get; set; } = "INFO";

    /// <summary>Id đối tượng liên quan (giáo án, lời mời...).</summary>
    public Guid? ReferenceId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
