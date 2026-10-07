using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.AdminDTOs;

namespace GYM_Management_System.Services.AdminServices;

public interface IAdminRoleService
{
    Task<List<RoleDto>> GetRolesAsync();

    Task<RoleDto> GetRoleByIdAsync(Guid roleId);

    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request);

    Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request);

    Task DeleteRoleAsync(Guid roleId);

    Task<List<PermissionDto>> GetPermissionsAsync();
}
