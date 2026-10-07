using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Một hiệp của bài tập: thời gian tập thật và thời gian nghỉ sau hiệp (do người dùng bấm).
/// </summary>
public partial class WorkoutSetLog
{
    public Guid WorkoutSetLogId { get; set; }

    public Guid WorkoutSessionExerciseId { get; set; }

    public int SetNumber { get; set; }

    /// <summary>Số lần thực hiện (nếu người dùng nhập).</summary>
    public int? Reps { get; set; }

    public int DurationSeconds { get; set; }

    public int RestSeconds { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    /// <summary>DONE / SKIPPED.</summary>
    public string Status { get; set; } = DomainConstants.WorkoutSessionStatus.Done;

    public virtual WorkoutSessionExercise SessionExercise { get; set; } = null!;
}
