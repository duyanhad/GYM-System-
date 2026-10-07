using System;
using System.Collections.Generic;

namespace GYM_Management_System.Models;

/// <summary>
/// Giáo án / buổi tập mẫu (UserId = null là giáo án mẫu của hệ thống).
/// </summary>
public partial class WorkoutPlan
{
    public Guid WorkoutPlanId { get; set; }

    public Guid? UserId { get; set; }

    public string Name { get; set; } = "";

    /// <summary>Nhóm cơ chính của buổi tập.</summary>
    public string? Focus { get; set; }

    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<WorkoutPlanItem> Items { get; set; } = new List<WorkoutPlanItem>();
}
