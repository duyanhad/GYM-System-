using System;
using System.ComponentModel.DataAnnotations;

namespace GYM_Management_System.DTOs.MembershipDTOs;

public class MembershipPlanDto
{
    public Guid PlanId { get; set; }
    public string PlanCode { get; set; } = "";
    public string PlanName { get; set; } = "";
    public string? Description { get; set; }
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public int? PersonalTrainingSessions { get; set; }
    public int MaxFreezeDays { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Số hội viên đang dùng gói này.</summary>
    public int ActiveSubscriptions { get; set; }
}

public class CreateMembershipPlanRequest
{
    [Required(ErrorMessage = "Mã gói tập là bắt buộc.")]
    [MaxLength(50)]
    public string PlanCode { get; set; } = "";

    [Required(ErrorMessage = "Tên gói tập là bắt buộc.")]
    [MaxLength(200)]
    public string PlanName { get; set; } = "";

    public string? Description { get; set; }

    [Range(1, 3650, ErrorMessage = "Thời hạn gói phải từ 1 đến 3650 ngày.")]
    public int DurationDays { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá gói tập không hợp lệ.")]
    public decimal Price { get; set; }

    public int? PersonalTrainingSessions { get; set; }
    public int MaxFreezeDays { get; set; }
    public Guid? BranchId { get; set; }
}

public class UpdateMembershipPlanRequest
{
    [MaxLength(200)]
    public string? PlanName { get; set; }

    public string? Description { get; set; }
    public int? DurationDays { get; set; }
    public decimal? Price { get; set; }
    public int? PersonalTrainingSessions { get; set; }
    public int? MaxFreezeDays { get; set; }
    public Guid? BranchId { get; set; }
    public bool? IsActive { get; set; }
}

public class MembershipPlanQueryParameters
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public Guid? BranchId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class MemberSubscriptionDto
{
    public Guid SubscriptionId { get; set; }
    public string SubscriptionCode { get; set; } = "";
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberCode { get; set; }
    public Guid PlanId { get; set; }
    public string? PlanName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PaymentStatus { get; set; } = "";
    public int RemainingPersonalTrainingSessions { get; set; }
    public DateTime? FreezeStartDate { get; set; }
    public DateTime? FreezeEndDate { get; set; }
    public int RemainingDays { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateSubscriptionRequest
{
    [Required(ErrorMessage = "Hội viên là bắt buộc.")]
    public Guid MemberId { get; set; }

    [Required(ErrorMessage = "Gói tập là bắt buộc.")]
    public Guid PlanId { get; set; }

    public Guid? BranchId { get; set; }

    /// <summary>Ngày bắt đầu (mặc định hôm nay).</summary>
    public DateTime? StartDate { get; set; }

    public decimal DiscountAmount { get; set; }

    /// <summary>Nếu true: tạo kèm hóa đơn cho gói tập vừa đăng ký.</summary>
    public bool CreateInvoice { get; set; } = true;

    public string? Notes { get; set; }
}

public class RenewSubscriptionRequest
{
    public Guid? PlanId { get; set; }
    public DateTime? StartDate { get; set; }
    public decimal DiscountAmount { get; set; }
    public bool CreateInvoice { get; set; } = true;
    public string? Notes { get; set; }
}

public class FreezeSubscriptionRequest
{
    [Range(1, 365, ErrorMessage = "Số ngày bảo lưu phải từ 1 đến 365.")]
    public int Days { get; set; }

    public string? Reason { get; set; }
}

public class CancelSubscriptionRequest
{
    public string? Reason { get; set; }
}

public class SubscriptionQueryParameters
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public Guid? MemberId { get; set; }
    public Guid? PlanId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
