using System;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.MembershipDTOs;
using GYM_Management_System.Services.MembershipServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.MembershipController;

/// <summary>
/// Quản lý gói tập (membership plans).
/// </summary>
[ApiController]
[Route("api/membership-plans")]
public class MembershipPlansController : ControllerBase
{
    private readonly IMembershipPlanService _planService;

    public MembershipPlansController(IMembershipPlanService planService)
    {
        _planService = planService;
    }

    [HasPermission(PermissionConstants.MEMBERSHIP_PLAN_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetPlans([FromQuery] MembershipPlanQueryParameters query)
    {
        var result = await _planService.GetPlansAsync(query);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.MEMBERSHIP_PLAN_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlanById(Guid id)
    {
        var result = await _planService.GetPlanByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.MEMBERSHIP_PLAN_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreateMembershipPlanRequest request)
    {
        var result = await _planService.CreatePlanAsync(request);
        return Ok(ApiResponse<MembershipPlanDto>.Ok(result, "Tạo gói tập thành công."));
    }

    [HasPermission(PermissionConstants.MEMBERSHIP_PLAN_UPDATE)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateMembershipPlanRequest request)
    {
        var result = await _planService.UpdatePlanAsync(id, request);
        return Ok(ApiResponse<MembershipPlanDto>.Ok(result, "Cập nhật gói tập thành công."));
    }

    [HasPermission(PermissionConstants.MEMBERSHIP_PLAN_UPDATE)]
    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> ChangeActive(Guid id, [FromQuery] bool isActive)
    {
        var result = await _planService.ChangeActiveAsync(id, isActive);
        return Ok(ApiResponse<MembershipPlanDto>.Ok(result, isActive ? "Đã mở bán gói tập." : "Đã ngừng bán gói tập."));
    }

    [HasPermission(PermissionConstants.MEMBERSHIP_PLAN_DELETE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        await _planService.DeletePlanAsync(id);
        return Ok(new { success = true, message = "Xóa gói tập thành công." });
    }
}
