using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.WorkoutDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Services.WorkoutServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.WorkoutController;

/// <summary>
/// Kết nối tài khoản và chia sẻ giáo án.
/// </summary>
[ApiController]
[Route("api/workout-shares")]
public class WorkoutSharesController : ControllerBase
{
    private readonly IWorkoutShareService _shareService;

    public WorkoutSharesController(IWorkoutShareService shareService)
    {
        _shareService = shareService;
    }

    [HasPermission(PermissionConstants.WORKOUT_SHARE)]
    [HttpGet("connections")]
    public async Task<IActionResult> GetConnections()
        => Ok(await _shareService.GetConnectionsAsync(CurrentUserId()));

    [HasPermission(PermissionConstants.WORKOUT_SHARE)]
    [HttpPost("connections/invite")]
    public async Task<IActionResult> Invite([FromBody] InviteConnectionRequest request)
    {
        var result = await _shareService.InviteConnectionAsync(CurrentUserId(), request);

        return Ok(ApiResponse<ConnectionDto>.Ok(result, "Đã gửi lời mời kết nối."));
    }

    [HasPermission(PermissionConstants.WORKOUT_SHARE)]
    [HttpPost("connections/{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id)
    {
        var result = await _shareService.AcceptConnectionAsync(CurrentUserId(), id);

        return Ok(ApiResponse<ConnectionDto>.Ok(result, "Đã chấp nhận kết nối."));
    }

    [HasPermission(PermissionConstants.WORKOUT_SHARE)]
    [HttpPost]
    public async Task<IActionResult> SharePlan([FromBody] ShareWorkoutPlanRequest request)
    {
        var result = await _shareService.SharePlanAsync(CurrentUserId(), request);

        return Ok(ApiResponse<WorkoutPlanShareDto>.Ok(result, "Đã chia sẻ giáo án."));
    }

    [HasPermission(PermissionConstants.WORKOUT_SHARE)]
    [HttpGet("received")]
    public async Task<IActionResult> GetReceived()
        => Ok(await _shareService.GetReceivedSharesAsync(CurrentUserId()));

    [HasPermission(PermissionConstants.WORKOUT_SHARE)]
    [HttpGet("sent")]
    public async Task<IActionResult> GetSent()
        => Ok(await _shareService.GetSentSharesAsync(CurrentUserId()));

    /// <summary>Nhận giáo án được chia sẻ thành giáo án của mình.</summary>
    [HasPermission(PermissionConstants.WORKOUT_SHARE)]
    [HttpPost("{shareId:guid}/import")]
    public async Task<IActionResult> Import(Guid shareId)
    {
        var result = await _shareService.ImportSharedPlanAsync(CurrentUserId(), shareId);

        return Ok(ApiResponse<WorkoutPlanDto>.Ok(result, "Đã thêm giáo án vào danh sách của bạn."));
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
        => Ok(await _shareService.GetNotificationsAsync(CurrentUserId(), unreadOnly));

    [HttpPost("notifications/read")]
    public async Task<IActionResult> MarkRead([FromQuery] Guid? id)
    {
        var count = await _shareService.MarkNotificationsReadAsync(CurrentUserId(), id);

        return Ok(new { success = true, message = $"Đã đánh dấu {count} thông báo là đã đọc." });
    }

    private Guid CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(raw, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Không xác định được người dùng đang đăng nhập.");
    }
}
