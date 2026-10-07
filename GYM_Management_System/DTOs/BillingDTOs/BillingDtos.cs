using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GYM_Management_System.DTOs.BillingDTOs;

public class InvoiceDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = "";
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberCode { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public Guid? SubscriptionId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
}

public class InvoiceItemDto
{
    public Guid InvoiceItemId { get; set; }
    public string ItemType { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public string ItemName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class CreateInvoiceItemRequest
{
    /// <summary>SUBSCRIPTION / CLASS / PERSONAL_TRAINING / PRODUCT / SERVICE.</summary>
    public string ItemType { get; set; } = Models.DomainConstants.InvoiceItemType.Service;

    public Guid? ReferenceId { get; set; }

    [Required(ErrorMessage = "Tên dịch vụ/hàng hóa là bắt buộc.")]
    [MaxLength(300)]
    public string ItemName { get; set; } = "";

    [Range(1, 1000, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int Quantity { get; set; } = 1;

    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không hợp lệ.")]
    public decimal UnitPrice { get; set; }

    public string? Notes { get; set; }
}

public class CreateInvoiceRequest
{
    public Guid? MemberId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }

    [MinLength(1, ErrorMessage = "Hóa đơn phải có ít nhất 1 dòng chi tiết.")]
    public List<CreateInvoiceItemRequest> Items { get; set; } = new();
}

public class InvoiceQueryParameters
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public Guid? MemberId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PaymentDto
{
    public Guid PaymentId { get; set; }
    public string PaymentCode { get; set; } = "";
    public Guid? InvoiceId { get; set; }
    public string? InvoiceCode { get; set; }
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string PaymentMethod { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Reference { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreatePaymentRequest
{
    /// <summary>Thu tiền cho hóa đơn nào (có thể null nếu thu lẻ / nạp trước).</summary>
    public Guid? InvoiceId { get; set; }

    public Guid? MemberId { get; set; }
    public Guid? BranchId { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Số tiền thu phải lớn hơn 0.")]
    public decimal Amount { get; set; }

    /// <summary>CASH / TRANSFER / CARD / EWALLET.</summary>
    public string PaymentMethod { get; set; } = Models.DomainConstants.PaymentMethod.Cash;

    public DateTime? PaymentDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class PaymentQueryParameters
{
    public string? Search { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? MemberId { get; set; }
    public Guid? BranchId { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class RevenueSummaryDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal SubscriptionRevenue { get; set; }
    public decimal ClassRevenue { get; set; }
    public decimal OtherRevenue { get; set; }
    public int PaidInvoiceCount { get; set; }
    public int UnpaidInvoiceCount { get; set; }
    public decimal OutstandingAmount { get; set; }
}
