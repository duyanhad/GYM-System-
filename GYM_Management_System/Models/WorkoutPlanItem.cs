using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Một bài tập trong giáo án, kèm thứ tự tập.
/// </summary>
public partial class WorkoutPlanItem
{
    public Guid WorkoutPlanItemId { get; set; }

    public Guid WorkoutPlanId { get; set; }

    /// <summary>Thứ tự tập trong buổi (1, 2, 3...).</summary>
    public int OrderIndex { get; set; }

    public Guid? ExerciseId { get; set; }

    public string ExerciseName { get; set; } = "";

    public int TargetSets { get; set; } = 3;

    public int TargetReps { get; set; } = 10;

    /// <summary>Thời gian nghỉ giữa các hiệp (giây).</summary>
    public int RestSeconds { get; set; } = 60;

    public string? Note { get; set; }

    public virtual WorkoutPlan WorkoutPlan { get; set; } = null!;
}
