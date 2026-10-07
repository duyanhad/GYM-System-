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
/// Phiếu thu tiền của hội viên.
/// </summary>
[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HasPermission(PermissionConstants.PAYMENT_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] PaymentQueryParameters query)
    {
        var result = await _paymentService.GetPaymentsAsync(query);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.PAYMENT_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPaymentById(Guid id)
    {
        var result = await _paymentService.GetPaymentByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.PAYMENT_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var result = await _paymentService.CreatePaymentAsync(request, GetCurrentUserId());
        return Ok(ApiResponse<PaymentDto>.Ok(result, "Ghi nhận thanh toán thành công."));
    }

    [HasPermission(PermissionConstants.PAYMENT_UPDATE)]
    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> RefundPayment(Guid id, [FromBody] RefundPaymentRequest request)
    {
        var result = await _paymentService.RefundPaymentAsync(id, request.Reason);
        return Ok(ApiResponse<PaymentDto>.Ok(result, "Hoàn tiền thành công."));
    }

    [HasPermission(PermissionConstants.PAYMENT_DELETE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePayment(Guid id)
    {
        await _paymentService.DeletePaymentAsync(id);
        return Ok(new { success = true, message = "Xóa phiếu thu thành công." });
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}

/// <summary>Yêu cầu hoàn tiền cho một phiếu thu.</summary>
public class RefundPaymentRequest
{
    public string? Reason { get; set; }
}
