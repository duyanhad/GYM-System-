using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Lịch học cố định hàng tuần của một lớp.
/// </summary>
public partial class ClassSchedule
{
    public Guid ScheduleId { get; set; }

    public Guid ClassId { get; set; }

    public Guid? TrainerId { get; set; }

    public string? Room { get; set; }

    /// <summary>0 = Chủ nhật ... 6 = Thứ bảy (theo DayOfWeek của .NET).</summary>
    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public virtual TrainingClass TrainingClass { get; set; } = null!;

    public virtual Trainer? Trainer { get; set; }

    public virtual ICollection<ClassBooking> Bookings { get; set; } = new List<ClassBooking>();
}
