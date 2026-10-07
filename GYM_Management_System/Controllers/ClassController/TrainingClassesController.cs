using System;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.ClassDTOs;
using GYM_Management_System.Services.ClassServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.ClassController;

/// <summary>
/// Quản lý lớp học / bộ môn.
/// </summary>
[ApiController]
[Route("api/training-classes")]
public class TrainingClassesController : ControllerBase
{
    private readonly ITrainingClassService _classService;

    public TrainingClassesController(ITrainingClassService classService)
    {
        _classService = classService;
    }

    [HasPermission(PermissionConstants.CLASS_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetClasses([FromQuery] TrainingClassQueryParameters query)
    {
        var result = await _classService.GetClassesAsync(query);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.CLASS_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetClassById(Guid id)
    {
        var result = await _classService.GetClassByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.CLASS_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateClass([FromBody] CreateTrainingClassRequest request)
    {
        var result = await _classService.CreateClassAsync(request);
        return Ok(ApiResponse<TrainingClassDto>.Ok(result, "Tạo lớp học thành công."));
    }

    [HasPermission(PermissionConstants.CLASS_UPDATE)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateClass(Guid id, [FromBody] UpdateTrainingClassRequest request)
    {
        var result = await _classService.UpdateClassAsync(id, request);
        return Ok(ApiResponse<TrainingClassDto>.Ok(result, "Cập nhật lớp học thành công."));
    }

    [HasPermission(PermissionConstants.CLASS_UPDATE)]
    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> ChangeActive(Guid id, [FromQuery] bool isActive)
    {
        var result = await _classService.ChangeActiveAsync(id, isActive);
        return Ok(ApiResponse<TrainingClassDto>.Ok(result, isActive ? "Đã mở lớp học." : "Đã ngừng lớp học."));
    }

    [HasPermission(PermissionConstants.CLASS_DELETE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteClass(Guid id)
    {
        await _classService.DeleteClassAsync(id);
        return Ok(new { success = true, message = "Xóa lớp học thành công." });
    }
}
