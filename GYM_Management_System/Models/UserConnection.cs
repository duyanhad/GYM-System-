using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Lời mời kết nối giữa hai tài khoản (để chia sẻ giáo án).
/// </summary>
public partial class UserConnection
{
    public Guid UserConnectionId { get; set; }

    public Guid RequesterUserId { get; set; }

    public Guid AddresseeUserId { get; set; }

    /// <summary>PENDING / ACCEPTED / REJECTED / CANCELLED.</summary>
    public string Status { get; set; } = DomainConstants.ConnectionStatus.Pending;

    public DateTime? CreatedAt { get; set; }

    public DateTime? RespondedAt { get; set; }
}
