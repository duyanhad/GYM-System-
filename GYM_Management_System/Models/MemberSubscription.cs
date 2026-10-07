using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Đăng ký gói tập của hội viên (hợp đồng dịch vụ).
/// </summary>
public partial class MemberSubscription
{
    public Guid SubscriptionId { get; set; }

    public string SubscriptionCode { get; set; } = "";

    public Guid MemberId { get; set; }

    public Guid PlanId { get; set; }

    public Guid? BranchId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>ACTIVE / EXPIRED / CANCELLED / FROZEN (xem <see cref="DomainConstants.SubscriptionStatus"/>).</summary>
    public string Status { get; set; } = DomainConstants.SubscriptionStatus.Active;

    public decimal TotalPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    /// <summary>UNPAID / PARTIAL / PAID (xem <see cref="DomainConstants.PaymentStatus"/>).</summary>
    public string PaymentStatus { get; set; } = DomainConstants.PaymentStatus.Unpaid;

    /// <summary>Số buổi PT còn lại của gói.</summary>
    public int RemainingPersonalTrainingSessions { get; set; }

    public DateTime? FreezeStartDate { get; set; }

    public DateTime? FreezeEndDate { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual MembershipPlan Plan { get; set; } = null!;

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
