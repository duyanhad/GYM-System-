using System;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.AdminDTOs;
using GYM_Management_System.Services.AdminServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.AdminController;

/// <summary>
/// Quản trị role và gán quyền theo role.
/// </summary>
[ApiController]
[Route("api/admin/roles")]
public class AdminRolesController : ControllerBase
{
    private readonly IAdminRoleService _roleService;

    public AdminRolesController(IAdminRoleService roleService)
    {
        _roleService = roleService;
    }

    [HasPermission(PermissionConstants.ADMIN_ROLE_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var result = await _roleService.GetRolesAsync();
        return Ok(result);
    }

    [HasPermission(PermissionConstants.ADMIN_ROLE_VIEW)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRoleById(Guid id)
    {
        var result = await _roleService.GetRoleByIdAsync(id);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.ADMIN_ROLE_CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var result = await _roleService.CreateRoleAsync(request);
        return Ok(ApiResponse<RoleDto>.Ok(result, "Tạo role thành công."));
    }

    [HasPermission(PermissionConstants.ADMIN_ROLE_UPDATE)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var result = await _roleService.UpdateRoleAsync(id, request);
        return Ok(ApiResponse<RoleDto>.Ok(result, "Cập nhật role thành công."));
    }

    [HasPermission(PermissionConstants.ADMIN_ROLE_DELETE)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        await _roleService.DeleteRoleAsync(id);
        return Ok(new { success = true, message = "Xóa role thành công." });
    }
}
