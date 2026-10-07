using System;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.ClassDTOs;
using GYM_Management_System.Services.ClassServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.ClassController;

/// <summary>
/// Lịch học hàng tuần của các lớp.
/// </summary>
[ApiController]
[Route("api/class-schedules")]
public class ClassSchedulesController : ControllerBase
{
    private readonly IClassScheduleService _scheduleService;

    public ClassSchedulesController(IClassScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HasPermission(PermissionConstants.CLASS_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetSchedules(
        [FromQuery] Guid? classId,
        [FromQuery] Guid? trainerId,
        [FromQuery] int? dayOfWeek,
        [FromQuery] bool? isActive)
    {
        var result = await _scheduleService.GetSchedulesAsync(classId, trainerId, dayOfWeek, isActive);
        return Ok(result);
    }

    /// <summary>Thời khóa biểu dạng lưới 7 ngày trong tuần.</summary>
    [HasPermission(PermissionConstants.CLASS_VIEW)]
    [HttpGet("timetable")]
    public async Task<IActionResult> GetWeeklyTimetable([FromQuery] Guid? branchId)
    {
        var result = await _scheduleService.GetWeeklyTimetableAsync(branchId);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.CLASS_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetScheduleById(Guid id)
    {
        var result = await _scheduleService.GetScheduleByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.CLASS_SCHEDULE_MANAGE)]
    [HttpPost]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateClassScheduleRequest request)
    {
        var result = await _scheduleService.CreateScheduleAsync(request);
        return Ok(ApiResponse<ClassScheduleDto>.Ok(result, "Thêm lịch học thành công."));
    }

    [HasPermission(PermissionConstants.CLASS_SCHEDULE_MANAGE)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] UpdateClassScheduleRequest request)
    {
        var result = await _scheduleService.UpdateScheduleAsync(id, request);
        return Ok(ApiResponse<ClassScheduleDto>.Ok(result, "Cập nhật lịch học thành công."));
    }

    [HasPermission(PermissionConstants.CLASS_SCHEDULE_MANAGE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        await _scheduleService.DeleteScheduleAsync(id);
        return Ok(new { success = true, message = "Xóa lịch học thành công." });
    }
}
