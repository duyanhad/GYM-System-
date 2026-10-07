using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Chi nhánh / cơ sở phòng gym.
/// </summary>
public partial class Branch
{
    public Guid BranchId { get; set; }

    public string BranchName { get; set; } = "";

    public string? BranchCode { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    /// <summary>Giờ mở cửa (vd 05:00).</summary>
    public TimeSpan? OpeningTime { get; set; }

    /// <summary>Giờ đóng cửa (vd 22:00).</summary>
    public TimeSpan? ClosingTime { get; set; }

    public Guid? ManagerUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? ManagerUser { get; set; }

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();

    public virtual ICollection<MembershipPlan> MembershipPlans { get; set; } = new List<MembershipPlan>();

    public virtual ICollection<MemberSubscription> Subscriptions { get; set; } = new List<MemberSubscription>();

    public virtual ICollection<Trainer> Trainers { get; set; } = new List<Trainer>();

    public virtual ICollection<TrainingClass> TrainingClasses { get; set; } = new List<TrainingClass>();

    public virtual ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
