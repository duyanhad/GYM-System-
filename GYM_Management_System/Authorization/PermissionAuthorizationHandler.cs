using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GYM_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Authorization;

/// <summary>
/// Handler trung tâm của hệ thống RBAC.
/// Quy tắc:
///   1. Role "Admin"  -> Succeed ngay lập tức (toàn quyền).
///   2. Role "Manager" -> Succeed mọi quyền TRỪ nhóm ADMIN_ (quản trị cấp cao).
///   3. Các role khác  -> kiểm tra Permission trong DB (UserPermission chặn/cấp thêm + RolePermission).
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly GymDbContext _dbContext;

    public PermissionAuthorizationHandler(GymDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var roles = context.User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        // 1. Admin: toàn quyền
        if (roles.Any(r => string.Equals(r, DomainConstants.AdminRole, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Manager: toàn quyền nghiệp vụ phòng gym, nhưng KHÔNG có quyền quản trị hệ thống
        var isManager = roles.Any(r => string.Equals(r, DomainConstants.ManagerRole, StringComparison.OrdinalIgnoreCase));
        if (isManager && !requirement.PermissionCode.StartsWith("ADMIN_"))
        {
            context.Succeed(requirement);
            return;
        }

        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            context.Fail();
            return;
        }

        // 3a. Chặn tường minh (deny) luôn thắng
        var hasExplicitDeny = await _dbContext.UserPermissions
            .AsNoTracking()
            .AnyAsync(up => up.UserId == userId
                && up.Permission.PermissionCode == requirement.PermissionCode
                && up.IsGranted == false);

        if (hasExplicitDeny)
        {
            context.Fail();
            return;
        }

        // 3b. Cấp quyền trực tiếp cho user
        var hasDirectPermission = await _dbContext.UserPermissions
            .AsNoTracking()
            .AnyAsync(up => up.UserId == userId
                && up.Permission.PermissionCode == requirement.PermissionCode
                && up.IsGranted != false);

        if (hasDirectPermission)
        {
            context.Succeed(requirement);
            return;
        }

        // 3c. Quyền theo role
        var hasRolePermission = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .AnyAsync(u => u.Roles.Any(r => r.Permissions.Any(p => p.PermissionCode == requirement.PermissionCode)));

        if (hasRolePermission)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}
