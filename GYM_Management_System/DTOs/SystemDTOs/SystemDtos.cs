using System;
using System.ComponentModel.DataAnnotations;

namespace GYM_Management_System.DTOs.SystemDTOs;

public class SystemLogDto
{
    public Guid LogId { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = "";
    public string? EntityName { get; set; }
    public Guid? EntityId { get; set; }
    public string? Description { get; set; }
    public string Level { get; set; } = "";
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SystemLogQueryParameters
{
    public string? Search { get; set; }
    public string? Level { get; set; }
    public string? EntityName { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SystemNotificationDto
{
    public Guid NotificationId { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "";
    public Guid? UserId { get; set; }
    public Guid? BranchId { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationRequest
{
    [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Nội dung là bắt buộc.")]
    [MaxLength(1000)]
    public string Message { get; set; } = "";

    /// <summary>INFO / WARNING / SUCCESS / ERROR.</summary>
    public string Type { get; set; } = "INFO";

    /// <summary>Người nhận cụ thể (null = gửi toàn hệ thống).</summary>
    public Guid? UserId { get; set; }

    public Guid? BranchId { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
}
