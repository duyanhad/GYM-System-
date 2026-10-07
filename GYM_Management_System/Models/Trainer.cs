using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Huấn luyện viên (PT) của phòng gym.
/// </summary>
public partial class Trainer
{
    public Guid TrainerId { get; set; }

    public string TrainerCode { get; set; } = "";

    public string FullName { get; set; } = "";

    public string? Email { get; set; }

    public string Phone { get; set; } = "";

    /// <summary>MALE / FEMALE / OTHER.</summary>
    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    /// <summary>Chuyên môn: Yoga, Boxing, Gym, Cardio, ...</summary>
    public string? Specialization { get; set; }

    public string? Bio { get; set; }

    /// <summary>Lương / phí theo giờ.</summary>
    public decimal HourlyRate { get; set; }

    public DateTime JoinDate { get; set; }

    public string? AvatarUrl { get; set; }

    /// <summary>Tài khoản đăng nhập tương ứng (nếu PT có tài khoản hệ thống).</summary>
    public Guid? UserId { get; set; }

    public Guid? BranchId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<TrainingClass> TrainingClasses { get; set; } = new List<TrainingClass>();

    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
}
