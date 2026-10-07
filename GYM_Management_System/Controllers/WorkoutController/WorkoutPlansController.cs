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
/// Giáo án (buổi tập): danh sách, tạo/sửa/xoá, gán vào lịch tuần.
/// </summary>
[ApiController]
[Route("api/workout-plans")]
public class WorkoutPlansController : ControllerBase
{
    private readonly IWorkoutPlanService _planService;

    public WorkoutPlansController(IWorkoutPlanService planService)
    {
        _planService = planService;
    }

    [HasPermission(PermissionConstants.WORKOUT_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetPlans()
        => Ok(await _planService.GetPlansAsync(CurrentUserId()));

    [HasPermission(PermissionConstants.WORKOUT_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlan(Guid id)
        => Ok(await _planService.GetPlanAsync(CurrentUserId(), id));

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] SaveWorkoutPlanRequest request)
    {
        var result = await _planService.SavePlanAsync(CurrentUserId(), request);

        return Ok(ApiResponse<WorkoutPlanDto>.Ok(result, "Lưu buổi tập thành công."));
    }

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] SaveWorkoutPlanRequest request)
    {
        var result = await _planService.SavePlanAsync(CurrentUserId(), request, id);

        return Ok(ApiResponse<WorkoutPlanDto>.Ok(result, "Cập nhật buổi tập thành công."));
    }

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        await _planService.DeletePlanAsync(CurrentUserId(), id);

        return Ok(new { success = true, message = "Xoá buổi tập thành công." });
    }

    [HasPermission(PermissionConstants.WORKOUT_VIEW)]
    [HttpGet("weekly-schedule")]
    public async Task<IActionResult> GetWeeklySchedule()
        => Ok(await _planService.GetWeeklyScheduleAsync(CurrentUserId()));

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpPut("weekly-schedule")]
    public async Task<IActionResult> AssignWeeklySchedule([FromBody] AssignWeeklyScheduleRequest request)
    {
        var result = await _planService.AssignWeeklyScheduleAsync(CurrentUserId(), request);

        var message = result.WorkoutPlanId.HasValue
            ? $"Đã xếp {result.PlanName} cho {result.DayName}."
            : $"Đã bỏ buổi tập của {result.DayName}.";

        return Ok(ApiResponse<WeeklyScheduleDto>.Ok(result, message));
    }

    /// <summary>Bài tập cần tập của một ngày (theo lịch tuần).</summary>
    [HasPermission(PermissionConstants.WORKOUT_VIEW)]
    [HttpGet("for-date/{dateKey}")]
    public async Task<IActionResult> GetItemsForDate(string dateKey)
    {
        var date = ParseDateKey(dateKey);

        var plan = await _planService.GetPlanForDateAsync(CurrentUserId(), date);
        var items = await _planService.ComposeItemsForDateAsync(CurrentUserId(), date);

        return Ok(new { dateKey, plan, items });
    }

    private static DateTime ParseDateKey(string dateKey)
        => DateTime.TryParse(dateKey, out var parsed)
            ? parsed.Date
            : throw new BusinessException("Ngày không đúng định dạng.");

    private Guid CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(raw, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Không xác định được người dùng đang đăng nhập.");
    }
}
