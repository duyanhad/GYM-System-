using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Lớp học / bộ môn (vd: Yoga cơ bản, Boxing nâng cao, Zumba).
/// </summary>
public partial class TrainingClass
{
    public Guid ClassId { get; set; }

    public string ClassCode { get; set; } = "";

    public string ClassName { get; set; } = "";

    public string? Description { get; set; }

    /// <summary>BEGINNER / INTERMEDIATE / ADVANCED (xem <see cref="DomainConstants.ClassLevel"/>).</summary>
    public string Level { get; set; } = DomainConstants.ClassLevel.Beginner;

    public Guid? TrainerId { get; set; }

    public Guid? BranchId { get; set; }

    /// <summary>Số học viên tối đa mỗi buổi.</summary>
    public int Capacity { get; set; }

    public int DurationMinutes { get; set; }

    public decimal PricePerSession { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Trainer? Trainer { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<ClassSchedule> Schedules { get; set; } = new List<ClassSchedule>();

    public virtual ICollection<ClassBooking> Bookings { get; set; } = new List<ClassBooking>();
}
