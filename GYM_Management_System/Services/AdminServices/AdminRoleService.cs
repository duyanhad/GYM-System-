using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.AdminDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.AdminServices;

public class AdminRoleService : IAdminRoleService
{
    private readonly GymDbContext _context;

    public AdminRoleService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Include(r => r.Users)
            .OrderBy(r => r.RoleName)
            .ToListAsync();

        return roles.Select(MapToDto).ToList();
    }

    public async Task<RoleDto> GetRoleByIdAsync(Guid roleId)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.RoleId == roleId)
            ?? throw new KeyNotFoundException("Không tìm thấy role.");

        return MapToDto(role);
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request)
    {
        var roleName = request.RoleName.Trim();

        if (await _context.Roles.AnyAsync(r => r.RoleName.ToLower() == roleName.ToLower()))
            throw new BusinessException("Role này đã tồn tại.");

        var permissions = await ResolvePermissionsAsync(request.Permissions);

        var role = new Role
        {
            RoleId = Guid.NewGuid(),
            RoleName = roleName,
            Description = request.Description,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            Permissions = permissions
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return MapToDto(role);
    }

    public async Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request)
    {
        var role = await _context.Roles
            .Include(r => r.Permissions)
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.RoleId == roleId)
            ?? throw new KeyNotFoundException("Không tìm thấy role.");

        if (request.Description != null)
            role.Description = request.Description;

        if (request.Permissions != null)
        {
            // Role hệ thống (Admin/Manager) giữ nguyên quyền để tránh tự khóa quyền quản trị
            if (role.IsSystem)
                throw new BusinessException("Role hệ thống không thể thay đổi danh sách quyền.");

            var permissions = await ResolvePermissionsAsync(request.Permissions);
            role.Permissions.Clear();
            foreach (var permission in permissions)
                role.Permissions.Add(permission);
        }

        await _context.SaveChangesAsync();

        return MapToDto(role);
    }

    public async Task DeleteRoleAsync(Guid roleId)
    {
        var role = await _context.Roles
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.RoleId == roleId)
            ?? throw new KeyNotFoundException("Không tìm thấy role.");

        if (role.IsSystem)
            throw new BusinessException("Role hệ thống không thể xóa.");

        if (role.Users.Count > 0)
            throw new BusinessException("Role đang được gán cho người dùng nên không thể xóa.");

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync()
    {
        return await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.GroupName).ThenBy(p => p.PermissionCode)
            .Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                PermissionCode = p.PermissionCode,
                PermissionName = p.PermissionName,
                GroupName = p.GroupName
            })
            .ToListAsync();
    }

    private async Task<List<Permission>> ResolvePermissionsAsync(List<string> permissionCodes)
    {
        if (permissionCodes == null || permissionCodes.Count == 0) return new List<Permission>();

        var codes = permissionCodes.Select(c => c.Trim()).Distinct().ToList();

        var permissions = await _context.Permissions
            .Where(p => codes.Contains(p.PermissionCode))
            .ToListAsync();

        var missing = codes.Except(permissions.Select(p => p.PermissionCode)).ToList();
        if (missing.Count > 0)
            throw new BusinessException($"Mã quyền không tồn tại: {string.Join(", ", missing)}.");

        return permissions;
    }

    private static RoleDto MapToDto(Role role) => new()
    {
        RoleId = role.RoleId,
        RoleName = role.RoleName,
        Description = role.Description,
        IsSystem = role.IsSystem,
        UserCount = role.Users?.Count ?? 0,
        Permissions = role.Permissions.Select(p => p.PermissionCode).OrderBy(c => c).ToList()
    };
}
