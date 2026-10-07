using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Lịch sử ra/vào phòng gym của hội viên (check-in / check-out).
/// </summary>
public partial class CheckIn
{
    public Guid CheckInId { get; set; }

    public Guid MemberId { get; set; }

    public Guid? BranchId { get; set; }

    public DateTime CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    /// <summary>QR / CODE / MANUAL (xem <see cref="DomainConstants.CheckInMethod"/>).</summary>
    public string Method { get; set; } = DomainConstants.CheckInMethod.Manual;

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual Branch? Branch { get; set; }
}
