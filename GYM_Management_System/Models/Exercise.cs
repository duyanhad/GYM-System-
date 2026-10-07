using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Bài tập trong thư viện (UserId = null nghĩa là bài tập mẫu dùng chung).
/// </summary>
public partial class Exercise
{
    public Guid ExerciseId { get; set; }

    /// <summary>Chủ sở hữu bài tập (null = bài tập mẫu của hệ thống).</summary>
    public Guid? UserId { get; set; }

    public string Name { get; set; } = "";

    /// <summary>Nhóm cơ: Ngực, Lưng, Chân, Vai, Tay, Bụng, Cardio.</summary>
    public string MuscleGroup { get; set; } = "";

    public int DefaultSets { get; set; } = 3;

    public int DefaultReps { get; set; } = 10;

    public int DefaultRestSeconds { get; set; } = 60;

    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
