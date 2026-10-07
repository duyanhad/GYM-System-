using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Đăng ký tham gia một buổi học của hội viên.
/// </summary>
public partial class ClassBooking
{
    public Guid BookingId { get; set; }

    public Guid ClassId { get; set; }

    public Guid? ScheduleId { get; set; }

    public Guid MemberId { get; set; }

    public DateTime BookedAt { get; set; }

    /// <summary>BOOKED / ATTENDED / CANCELLED / NO_SHOW (xem <see cref="DomainConstants.BookingStatus"/>).</summary>
    public string Status { get; set; } = DomainConstants.BookingStatus.Booked;

    public DateTime? CancelledAt { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public virtual TrainingClass TrainingClass { get; set; } = null!;

    public virtual ClassSchedule? Schedule { get; set; }

    public virtual Member Member { get; set; } = null!;
}
