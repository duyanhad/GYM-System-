using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.AttendanceDTOs;
using GYM_Management_System.Services.AttendanceServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.AttendanceController;

/// <summary>
/// Điểm danh ra/vào phòng gym (check-in, check-out) và thống kê lưu lượng.
/// </summary>
[ApiController]
[Route("api/check-ins")]
public class CheckInsController : ControllerBase
{
    private readonly ICheckInService _checkInService;

    public CheckInsController(ICheckInService checkInService)
    {
        _checkInService = checkInService;
    }

    [HasPermission(PermissionConstants.CHECKIN_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetCheckIns([FromQuery] AttendanceQueryParameters query)
    {
        var result = await _checkInService.GetCheckInsAsync(query);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.CHECKIN_VIEW)]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] DateTime? date, [FromQuery] Guid? branchId)
    {
        var result = await _checkInService.GetStatsAsync(date, branchId);
        return Ok(result);
    }

    /// <summary>Check-in hội viên bằng mã hội viên / số điện thoại / quét QR.</summary>
    [HasPermission(PermissionConstants.CHECKIN_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CheckIn([FromBody] CreateCheckInRequest request)
    {
        var result = await _checkInService.CheckInAsync(request, GetCurrentUserId());
        return Ok(ApiResponse<CheckInDto>.Ok(result, $"Check-in thành công cho {result.MemberName}."));
    }

    [HasPermission(PermissionConstants.CHECKIN_UPDATE)]
    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
    {
        var result = await _checkInService.CheckOutAsync(request);
        return Ok(ApiResponse<CheckInDto>.Ok(result, "Check-out thành công."));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
