using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.Services.AdminServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.AdminController;

/// <summary>
/// Danh mục toàn bộ quyền (permission) của hệ thống - dùng để dựng màn hình phân quyền.
/// </summary>
[ApiController]
[Route("api/admin/permissions")]
public class AdminPermissionsController : ControllerBase
{
    private readonly IAdminRoleService _roleService;

    public AdminPermissionsController(IAdminRoleService roleService)
    {
        _roleService = roleService;
    }

    [HasPermission(PermissionConstants.ADMIN_ROLE_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetPermissions()
    {
        var result = await _roleService.GetPermissionsAsync();
        return Ok(result);
    }
}
