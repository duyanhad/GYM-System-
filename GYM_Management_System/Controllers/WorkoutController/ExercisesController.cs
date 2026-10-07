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
/// Thư viện bài tập: bài tập mẫu của hệ thống + bài tập riêng của người dùng.
/// </summary>
[ApiController]
[Route("api/exercises")]
public class ExercisesController : ControllerBase
{
    private readonly IWorkoutExerciseService _exerciseService;

    public ExercisesController(IWorkoutExerciseService exerciseService)
    {
        _exerciseService = exerciseService;
    }

    [HasPermission(PermissionConstants.WORKOUT_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetExercises()
        => Ok(await _exerciseService.GetExercisesAsync(CurrentUserId()));

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpPost]
    public async Task<IActionResult> CreateExercise([FromBody] CreateExerciseRequest request)
    {
        var result = await _exerciseService.CreateExerciseAsync(CurrentUserId(), request);

        return Ok(ApiResponse<ExerciseDto>.Ok(result, "Thêm bài tập thành công."));
    }

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateExercise(Guid id, [FromBody] UpdateExerciseRequest request)
    {
        var result = await _exerciseService.UpdateExerciseAsync(CurrentUserId(), id, request);

        return Ok(ApiResponse<ExerciseDto>.Ok(result, "Cập nhật bài tập thành công."));
    }

    [HasPermission(PermissionConstants.WORKOUT_MANAGE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteExercise(Guid id)
    {
        await _exerciseService.DeleteExerciseAsync(CurrentUserId(), id);

        return Ok(new { success = true, message = "Xoá bài tập thành công." });
    }

    private Guid CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(raw, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Không xác định được người dùng đang đăng nhập.");
    }
}
