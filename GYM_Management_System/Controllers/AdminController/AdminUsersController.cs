using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.AdminDTOs;
using GYM_Management_System.Services.AdminServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.AdminController;

/// <summary>
/// Quản trị tài khoản người dùng hệ thống (Admin / Manager / Lễ tân / PT).
/// </summary>
[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _userService;

    public AdminUsersController(IAdminUserService userService)
    {
        _userService = userService;
    }

    [HasPermission(PermissionConstants.ADMIN_USER_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] UserQueryParameters query)
    {
        var result = await _userService.GetUsersAsync(query);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.ADMIN_USER_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.ADMIN_USER_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request, GetCurrentUserId());
        return Ok(ApiResponse<SystemUserDto>.Ok(result, "Tạo tài khoản thành công."));
    }

    [HasPermission(PermissionConstants.ADMIN_USER_UPDATE)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var result = await _userService.UpdateUserAsync(id, request);
        return Ok(ApiResponse<SystemUserDto>.Ok(result, "Cập nhật tài khoản thành công."));
    }

    [HasPermission(PermissionConstants.ADMIN_USER_UPDATE)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeUserStatusRequest request)
    {
        var result = await _userService.ChangeStatusAsync(id, request);
        return Ok(ApiResponse<SystemUserDto>.Ok(result, "Cập nhật trạng thái tài khoản thành công."));
    }

    [HasPermission(PermissionConstants.ADMIN_USER_UPDATE)]
    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordRequest request)
    {
        await _userService.ResetPasswordAsync(id, request);
        return Ok(new { success = true, message = "Đặt lại mật khẩu thành công." });
    }

    [HasPermission(PermissionConstants.ADMIN_PERMISSION_ASSIGN)]
    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignUserPermissionsRequest request)
    {
        await _userService.AssignPermissionsAsync(id, request);
        return Ok(new { success = true, message = "Cập nhật quyền riêng cho tài khoản thành công." });
    }

    [HasPermission(PermissionConstants.ADMIN_USER_DELETE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _userService.DeleteUserAsync(id);
        return Ok(new { success = true, message = "Xóa tài khoản thành công." });
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
