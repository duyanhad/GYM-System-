using System;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.TrainerDTOs;
using GYM_Management_System.Services.TrainerServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.TrainerController;

/// <summary>
/// Quản lý huấn luyện viên (PT).
/// </summary>
[ApiController]
[Route("api/trainers")]
public class TrainersController : ControllerBase
{
    private readonly ITrainerService _trainerService;

    public TrainersController(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    [HasPermission(PermissionConstants.TRAINER_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetTrainers([FromQuery] TrainerQueryParameters query)
    {
        var result = await _trainerService.GetTrainersAsync(query);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.TRAINER_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTrainerById(Guid id)
    {
        var result = await _trainerService.GetTrainerByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.TRAINER_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateTrainer([FromBody] CreateTrainerRequest request)
    {
        var result = await _trainerService.CreateTrainerAsync(request);
        return Ok(ApiResponse<TrainerDto>.Ok(result, "Thêm huấn luyện viên thành công."));
    }

    [HasPermission(PermissionConstants.TRAINER_UPDATE)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTrainer(Guid id, [FromBody] UpdateTrainerRequest request)
    {
        var result = await _trainerService.UpdateTrainerAsync(id, request);
        return Ok(ApiResponse<TrainerDto>.Ok(result, "Cập nhật huấn luyện viên thành công."));
    }

    [HasPermission(PermissionConstants.TRAINER_UPDATE)]
    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> ChangeActive(Guid id, [FromQuery] bool isActive)
    {
        var result = await _trainerService.ChangeActiveAsync(id, isActive);
        return Ok(ApiResponse<TrainerDto>.Ok(result, isActive ? "Đã kích hoạt huấn luyện viên." : "Đã tạm ngưng huấn luyện viên."));
    }

    [HasPermission(PermissionConstants.TRAINER_DELETE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTrainer(Guid id)
    {
        await _trainerService.DeleteTrainerAsync(id);
        return Ok(new { success = true, message = "Xóa huấn luyện viên thành công." });
    }
}
