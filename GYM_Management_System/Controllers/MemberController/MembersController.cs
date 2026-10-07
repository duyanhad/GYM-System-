using System;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.MemberDTOs;
using GYM_Management_System.Services.MemberServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.MemberController;

/// <summary>
/// Quản lý hội viên: danh sách, chi tiết, thêm/sửa/xóa, trạng thái, lịch sử gói tập.
/// </summary>
[ApiController]
[Route("api/members")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;

    public MembersController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    [HasPermission(PermissionConstants.MEMBER_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetMembers([FromQuery] MemberQueryParameters query)
    {
        var result = await _memberService.GetMembersAsync(query);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.MEMBER_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMemberById(Guid id)
    {
        var result = await _memberService.GetMemberByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.MEMBER_VIEW)]
    [HttpGet("{id:guid}/subscriptions")]
    public async Task<IActionResult> GetMemberSubscriptions(Guid id)
    {
        var result = await _memberService.GetSubscriptionHistoryAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.MEMBER_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateMember([FromBody] CreateMemberRequest request)
    {
        var result = await _memberService.CreateMemberAsync(request);
        return Ok(ApiResponse<MemberDto>.Ok(result, "Thêm hội viên thành công."));
    }

    [HasPermission(PermissionConstants.MEMBER_UPDATE)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMember(Guid id, [FromBody] UpdateMemberRequest request)
    {
        var result = await _memberService.UpdateMemberAsync(id, request);
        return Ok(ApiResponse<MemberDto>.Ok(result, "Cập nhật hội viên thành công."));
    }

    [HasPermission(PermissionConstants.MEMBER_UPDATE)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeMemberStatusRequest request)
    {
        var result = await _memberService.ChangeStatusAsync(id, request);
        return Ok(ApiResponse<MemberDto>.Ok(result, "Cập nhật trạng thái hội viên thành công."));
    }

    [HasPermission(PermissionConstants.MEMBER_DELETE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMember(Guid id)
    {
        await _memberService.DeleteMemberAsync(id);
        return Ok(new { success = true, message = "Xóa hội viên thành công." });
    }
}
