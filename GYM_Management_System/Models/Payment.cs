using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Phiếu thu tiền của hội viên (có thể gắn với hóa đơn hoặc thu lẻ).
/// </summary>
public partial class Payment
{
    public Guid PaymentId { get; set; }

    public string PaymentCode { get; set; } = "";

    public Guid? InvoiceId { get; set; }

    public Guid? MemberId { get; set; }

    public Guid? BranchId { get; set; }

    /// <summary>CASH / TRANSFER / CARD / EWALLET (xem <see cref="DomainConstants.PaymentMethod"/>).</summary>
    public string PaymentMethod { get; set; } = DomainConstants.PaymentMethod.Cash;

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    /// <summary>Mã giao dịch / mã tham chiếu ngân hàng.</summary>
    public string? Reference { get; set; }

    /// <summary>SUCCESS / PENDING / FAILED / REFUNDED (xem <see cref="DomainConstants.PaymentStatus"/>).</summary>
    public string Status { get; set; } = DomainConstants.PaymentStatus.Success;

    public string? Notes { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual Member? Member { get; set; }

    public virtual Branch? Branch { get; set; }
}
