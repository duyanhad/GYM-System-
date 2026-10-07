using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Một buổi tập của người dùng trong một ngày.
/// </summary>
public partial class WorkoutSession
{
    public Guid WorkoutSessionId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Ngày tập (theo giờ Việt Nam, 00:00).</summary>
    public DateTime SessionDate { get; set; }

    public Guid? WorkoutPlanId { get; set; }

    public string? PlanName { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    /// <summary>Tổng thời gian tập (giây) - tổng thời gian của tất cả các hiệp.</summary>
    public int TotalDurationSeconds { get; set; }

    /// <summary>Tổng thời gian nghỉ (giây).</summary>
    public int TotalRestSeconds { get; set; }

    /// <summary>IN_PROGRESS / FINISHED / SKIPPED.</summary>
    public string Status { get; set; } = DomainConstants.WorkoutSessionStatus.InProgress;

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<WorkoutSessionExercise> Exercises { get; set; } = new List<WorkoutSessionExercise>();
}
