using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Hội viên tập luyện tại phòng gym.
/// </summary>
public partial class Member
{
    public Guid MemberId { get; set; }

    public string MemberCode { get; set; } = "";

    public string FullName { get; set; } = "";

    public string? Email { get; set; }

    public string Phone { get; set; } = "";

    /// <summary>MALE / FEMALE / OTHER.</summary>
    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? AvatarUrl { get; set; }

    /// <summary>Người liên hệ khẩn cấp.</summary>
    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public DateTime JoinDate { get; set; }

    /// <summary>ACTIVE / INACTIVE / SUSPENDED (xem <see cref="DomainConstants.MemberStatus"/>).</summary>
    public string Status { get; set; } = DomainConstants.MemberStatus.Active;

    public string? Notes { get; set; }

    public Guid? BranchId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<MemberSubscription> Subscriptions { get; set; } = new List<MemberSubscription>();

    public virtual ICollection<ClassBooking> ClassBookings { get; set; } = new List<ClassBooking>();

    public virtual ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
