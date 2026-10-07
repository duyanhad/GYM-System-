using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Hóa đơn bán gói tập / lớp học / dịch vụ cho hội viên.
/// </summary>
public partial class Invoice
{
    public Guid InvoiceId { get; set; }

    public string InvoiceCode { get; set; } = "";

    public Guid? MemberId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? SubscriptionId { get; set; }

    public DateTime InvoiceDate { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    /// <summary>UNPAID / PARTIAL / PAID / CANCELLED (xem <see cref="DomainConstants.InvoiceStatus"/>).</summary>
    public string Status { get; set; } = DomainConstants.InvoiceStatus.Unpaid;

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Member? Member { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual MemberSubscription? Subscription { get; set; }

    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
