using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.ClassDTOs;
using GYM_Management_System.Services.ClassServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.ClassController;

/// <summary>
/// Đăng ký lớp học của hội viên.
/// </summary>
[ApiController]
[Route("api/class-bookings")]
public class ClassBookingsController : ControllerBase
{
    private readonly IClassBookingService _bookingService;

    public ClassBookingsController(IClassBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HasPermission(PermissionConstants.BOOKING_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetBookings([FromQuery] ClassBookingQueryParameters query)
    {
        var result = await _bookingService.GetBookingsAsync(query);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.BOOKING_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBookingById(Guid id)
    {
        var result = await _bookingService.GetBookingByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.BOOKING_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var result = await _bookingService.CreateBookingAsync(request, GetCurrentUserId());
        return Ok(ApiResponse<ClassBookingDto>.Ok(result, "Đăng ký lớp học thành công."));
    }

    [HasPermission(PermissionConstants.BOOKING_UPDATE)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBookingStatusRequest request)
    {
        var result = await _bookingService.UpdateStatusAsync(id, request);
        return Ok(ApiResponse<ClassBookingDto>.Ok(result, "Cập nhật trạng thái đăng ký thành công."));
    }

    [HasPermission(PermissionConstants.BOOKING_CANCEL)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        var result = await _bookingService.CancelBookingAsync(id);
        return Ok(ApiResponse<ClassBookingDto>.Ok(result, "Hủy đăng ký lớp học thành công."));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
