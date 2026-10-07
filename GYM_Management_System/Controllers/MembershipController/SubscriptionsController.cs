using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.MembershipDTOs;
using GYM_Management_System.Services.MembershipServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.MembershipController;

/// <summary>
/// Đăng ký / gia hạn / bảo lưu / hủy gói tập của hội viên.
/// </summary>
[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HasPermission(PermissionConstants.SUBSCRIPTION_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetSubscriptions([FromQuery] SubscriptionQueryParameters query)
    {
        var result = await _subscriptionService.GetSubscriptionsAsync(query);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.SUBSCRIPTION_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSubscriptionById(Guid id)
    {
        var result = await _subscriptionService.GetSubscriptionByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.SUBSCRIPTION_VIEW)]
    [HttpGet("members/{memberId:guid}/active")]
    public async Task<IActionResult> GetActiveSubscription(Guid memberId)
    {
        var result = await _subscriptionService.GetActiveSubscriptionAsync(memberId);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.SUBSCRIPTION_VIEW)]
    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiringSubscriptions([FromQuery] int days = 7)
    {
        var result = await _subscriptionService.GetExpiringSubscriptionsAsync(days);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.SUBSCRIPTION_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var result = await _subscriptionService.CreateSubscriptionAsync(request, GetCurrentUserId());
        return Ok(ApiResponse<MemberSubscriptionDto>.Ok(result, "Đăng ký gói tập thành công."));
    }

    [HasPermission(PermissionConstants.SUBSCRIPTION_UPDATE)]
    [HttpPost("{id:guid}/renew")]
    public async Task<IActionResult> RenewSubscription(Guid id, [FromBody] RenewSubscriptionRequest request)
    {
        var result = await _subscriptionService.RenewSubscriptionAsync(id, request, GetCurrentUserId());
        return Ok(ApiResponse<MemberSubscriptionDto>.Ok(result, "Gia hạn gói tập thành công."));
    }

    [HasPermission(PermissionConstants.SUBSCRIPTION_FREEZE)]
    [HttpPost("{id:guid}/freeze")]
    public async Task<IActionResult> FreezeSubscription(Guid id, [FromBody] FreezeSubscriptionRequest request)
    {
        var result = await _subscriptionService.FreezeSubscriptionAsync(id, request);
        return Ok(ApiResponse<MemberSubscriptionDto>.Ok(result, "Bảo lưu gói tập thành công."));
    }

    [HasPermission(PermissionConstants.SUBSCRIPTION_FREEZE)]
    [HttpPost("{id:guid}/unfreeze")]
    public async Task<IActionResult> UnfreezeSubscription(Guid id)
    {
        var result = await _subscriptionService.UnfreezeSubscriptionAsync(id);
        return Ok(ApiResponse<MemberSubscriptionDto>.Ok(result, "Mở bảo lưu gói tập thành công."));
    }

    [HasPermission(PermissionConstants.SUBSCRIPTION_CANCEL)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelSubscription(Guid id, [FromBody] CancelSubscriptionRequest request)
    {
        var result = await _subscriptionService.CancelSubscriptionAsync(id, request);
        return Ok(ApiResponse<MemberSubscriptionDto>.Ok(result, "Hủy gói tập thành công."));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
