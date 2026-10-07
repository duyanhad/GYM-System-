using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Chia sẻ giáo án cho một tài khoản đã kết nối.
/// </summary>
public partial class WorkoutPlanShare
{
    public Guid WorkoutPlanShareId { get; set; }

    public Guid WorkoutPlanId { get; set; }

    public Guid FromUserId { get; set; }

    /// <summary>Tài khoản nhận giáo án.</summary>
    public Guid ToUserId { get; set; }

    public string? Message { get; set; }

    public DateTime SharedAt { get; set; }
}
