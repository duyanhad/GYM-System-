using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.BillingDTOs;
using GYM_Management_System.Services.BillingServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.BillingController;

/// <summary>
/// Hóa đơn bán gói tập / lớp học / dịch vụ.
/// </summary>
[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HasPermission(PermissionConstants.INVOICE_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetInvoices([FromQuery] InvoiceQueryParameters query)
    {
        var result = await _invoiceService.GetInvoicesAsync(query);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.INVOICE_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetInvoiceById(Guid id)
    {
        var result = await _invoiceService.GetInvoiceByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.REPORT_VIEW)]
    [HttpGet("revenue-summary")]
    public async Task<IActionResult> GetRevenueSummary([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var result = await _invoiceService.GetRevenueSummaryAsync(fromDate, toDate);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.INVOICE_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        var result = await _invoiceService.CreateInvoiceAsync(request, GetCurrentUserId());
        return Ok(ApiResponse<InvoiceDto>.Ok(result, "Tạo hóa đơn thành công."));
    }

    [HasPermission(PermissionConstants.INVOICE_CANCEL)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelInvoice(Guid id)
    {
        var result = await _invoiceService.CancelInvoiceAsync(id);
        return Ok(ApiResponse<InvoiceDto>.Ok(result, "Hủy hóa đơn thành công."));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
