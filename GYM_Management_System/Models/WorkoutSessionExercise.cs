using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Một bài tập trong buổi tập, kèm thứ tự tập và trạng thái hoàn thành.
/// </summary>
public partial class WorkoutSessionExercise
{
    public Guid WorkoutSessionExerciseId { get; set; }

    public Guid WorkoutSessionId { get; set; }

    /// <summary>Thứ tự tập trong buổi (1, 2, 3...).</summary>
    public int OrderIndex { get; set; }

    public Guid? ExerciseId { get; set; }

    public string ExerciseName { get; set; } = "";

    public string? MuscleGroup { get; set; }

    public int TargetSets { get; set; } = 3;

    public int TargetReps { get; set; } = 10;

    public int RestSeconds { get; set; } = 60;

    public string? Note { get; set; }

    /// <summary>Tổng thời gian của bài này (giây).</summary>
    public int TotalWorkSeconds { get; set; }

    public int TotalExerciseRestSeconds { get; set; }

    public bool IsCompleted { get; set; }

    public virtual WorkoutSession WorkoutSession { get; set; } = null!;

    public virtual ICollection<WorkoutSetLog> Sets { get; set; } = new List<WorkoutSetLog>();
}
