using System;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.BranchDTOs;
using GYM_Management_System.Services.BranchServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.BranchController;

/// <summary>
/// Quản lý chi nhánh / cơ sở phòng gym.
/// </summary>
[ApiController]
[Route("api/branches")]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HasPermission(PermissionConstants.BRANCH_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetBranches([FromQuery] bool? isActive)
    {
        var result = await _branchService.GetBranchesAsync(isActive);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.BRANCH_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBranchById(Guid id)
    {
        var result = await _branchService.GetBranchByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.BRANCH_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request)
    {
        var result = await _branchService.CreateBranchAsync(request);
        return Ok(ApiResponse<BranchDto>.Ok(result, "Tạo chi nhánh thành công."));
    }

    [HasPermission(PermissionConstants.BRANCH_UPDATE)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBranch(Guid id, [FromBody] UpdateBranchRequest request)
    {
        var result = await _branchService.UpdateBranchAsync(id, request);
        return Ok(ApiResponse<BranchDto>.Ok(result, "Cập nhật chi nhánh thành công."));
    }

    [HasPermission(PermissionConstants.BRANCH_DELETE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBranch(Guid id)
    {
        await _branchService.DeleteBranchAsync(id);
        return Ok(new { success = true, message = "Xóa chi nhánh thành công." });
    }
}
