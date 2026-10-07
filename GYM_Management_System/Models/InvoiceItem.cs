using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Dòng chi tiết của hóa đơn.
/// </summary>
public partial class InvoiceItem
{
    public Guid InvoiceItemId { get; set; }

    public Guid InvoiceId { get; set; }

    /// <summary>SUBSCRIPTION / CLASS / PERSONAL_TRAINING / PRODUCT / SERVICE (xem <see cref="DomainConstants.InvoiceItemType"/>).</summary>
    public string ItemType { get; set; } = DomainConstants.InvoiceItemType.Service;

    /// <summary>Id của gói tập / lớp học / sản phẩm tương ứng (nếu có).</summary>
    public Guid? ReferenceId { get; set; }

    public string ItemName { get; set; } = "";

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public string? Notes { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
