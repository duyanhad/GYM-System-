using System;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.SystemDTOs;
using GYM_Management_System.Services.SystemServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.SystemController;

/// <summary>
/// Thông báo trong hệ thống.
/// </summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
public class SystemNotificationsController : ControllerBase
{
    private readonly ISystemNotificationService _notificationService;

    public SystemNotificationsController(ISystemNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Lấy danh sách thông báo của tài khoản đang đăng nhập.</summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int take = 50)
    {
        var result = await _notificationService.GetNotificationsAsync(unreadOnly, take);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.SYSTEM_NOTIFICATION_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        var result = await _notificationService.CreateAsync(request);
        return Ok(ApiResponse<SystemNotificationDto>.Ok(result, "Gửi thông báo thành công."));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Ok(new { success = true, message = "Đã đánh dấu thông báo là đã đọc." });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var count = await _notificationService.MarkAllAsReadAsync();
        return Ok(new { success = true, message = $"Đã đánh dấu {count} thông báo là đã đọc." });
    }
}
