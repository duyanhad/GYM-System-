using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Gói tập (Membership plan) - vd: Gói 1 tháng, Gói 6 tháng, Gói PT 12 buổi.
/// </summary>
public partial class MembershipPlan
{
    public Guid PlanId { get; set; }

    public string PlanCode { get; set; } = "";

    public string PlanName { get; set; } = "";

    public string? Description { get; set; }

    /// <summary>Thời hạn gói tính theo ngày.</summary>
    public int DurationDays { get; set; }

    /// <summary>Giá niêm yết.</summary>
    public decimal Price { get; set; }

    /// <summary>Số buổi kèm PT (null = không áp dụng).</summary>
    public int? PersonalTrainingSessions { get; set; }

    /// <summary>Số ngày tối đa được bảo lưu (freeze) gói.</summary>
    public int MaxFreezeDays { get; set; }

    public Guid? BranchId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<MemberSubscription> Subscriptions { get; set; } = new List<MemberSubscription>();
}
