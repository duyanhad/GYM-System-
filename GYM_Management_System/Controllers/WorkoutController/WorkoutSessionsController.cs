using System;
using System.Globalization;
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
/// Buổi tập của người dùng: bắt đầu buổi, ghi từng hiệp (thời gian tập + giờ nghỉ),
/// đánh dấu hoàn thành, kết thúc, lịch sử và thống kê.
/// </summary>
[ApiController]
[Route("api/workout-sessions")]
public class WorkoutSessionsController : ControllerBase
{
    private readonly IWorkoutSessionService _sessionService;

    public WorkoutSessionsController(IWorkoutSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HasPermission(PermissionConstants.WORKOUT_VIEW)]
    [HttpGet("by-date/{dateKey}")]
    public async Task<IActionResult> GetByDate(string dateKey)
    {
        var session = await _sessionService.GetSessionByDateAsync(CurrentUserId(), dateKey);

        return Ok(session);
    }

    [HasPermission(PermissionConstants.WORKOUT_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _sessionService.GetSessionByIdAsync(CurrentUserId(), id));

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request)
    {
        var result = await _sessionService.StartSessionAsync(CurrentUserId(), request);

        return Ok(ApiResponse<WorkoutSessionDto>.Ok(result, "Đã bắt đầu buổi tập."));
    }

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpPost("{id:guid}/exercises/{sessionExerciseId:guid}/sets")]
    public async Task<IActionResult> LogSet(Guid id, Guid sessionExerciseId, [FromBody] LogSetRequest request)
    {
        var result = await _sessionService.LogSetAsync(CurrentUserId(), id, sessionExerciseId, request);

        return Ok(ApiResponse<WorkoutSessionDto>.Ok(result, "Đã ghi lại hiệp tập."));
    }

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpPatch("exercises/{sessionExerciseId:guid}/completed")]
    public async Task<IActionResult> SetExerciseCompleted(Guid sessionExerciseId, [FromBody] SetExerciseCompletedRequest request)
    {
        var result = await _sessionService.SetExerciseCompletedAsync(CurrentUserId(), sessionExerciseId, request.IsCompleted);

        return Ok(ApiResponse<WorkoutSessionDto>.Ok(result, request.IsCompleted ? "Đã đánh dấu hoàn thành bài tập." : "Đã bỏ đánh dấu bài tập."));
    }

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpPost("{id:guid}/exercises/reorder")]
    public async Task<IActionResult> Reorder(Guid id, [FromBody] ReorderExercisesRequest request)
    {
        var result = await _sessionService.ReorderExercisesAsync(CurrentUserId(), id, request);

        return Ok(ApiResponse<WorkoutSessionDto>.Ok(result, "Đã lưu thứ tự bài tập."));
    }

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpPost("{id:guid}/finish")]
    public async Task<IActionResult> Finish(Guid id, [FromBody] FinishSessionRequest request)
    {
        var result = await _sessionService.FinishSessionAsync(CurrentUserId(), id, request);

        return Ok(ApiResponse<WorkoutSessionDto>.Ok(result, "Đã kết thúc buổi tập."));
    }

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _sessionService.DeleteSessionAsync(CurrentUserId(), id);

        return Ok(new { success = true, message = "Đã xoá buổi tập." });
    }

    [HasPermission(PermissionConstants.WORKOUT_VIEW)]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] WorkoutHistoryQueryParameters query)
        => Ok(await _sessionService.GetHistoryAsync(CurrentUserId(), query));

    /// <summary>Đánh dấu các ngày đã tập (dùng cho lịch tháng).</summary>
    [HasPermission(PermissionConstants.WORKOUT_VIEW)]
    [HttpGet("day-marks")]
    public async Task<IActionResult> GetDayMarks([FromQuery] string? fromDate, [FromQuery] string? toDate)
    {
        var from = ParseDate(fromDate) ?? DateTime.Now.Date.AddMonths(-1);
        var to = ParseDate(toDate) ?? DateTime.Now.Date;

        return Ok(await _sessionService.GetDayMarksAsync(CurrentUserId(), from, to));
    }

    [HasPermission(PermissionConstants.WORKOUT_VIEW)]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
        => Ok(await _sessionService.GetStatsAsync(CurrentUserId()));

    private static DateTime? ParseDate(string? value)
        => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : null;

    private Guid CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(raw, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Không xác định được người dùng đang đăng nhập.");
    }
}
