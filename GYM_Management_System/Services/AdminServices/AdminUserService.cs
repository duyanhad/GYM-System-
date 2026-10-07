using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.AdminDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.AdminServices;

public class AdminUserService : IAdminUserService
{
    private readonly GymDbContext _context;

    public AdminUserService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<SystemUserDto>> GetUsersAsync(UserQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var usersQuery = _context.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.DefaultBranch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Username.ToLower().Contains(keyword) ||
                u.FullName.ToLower().Contains(keyword) ||
                u.Email.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
            usersQuery = usersQuery.Where(u => u.Roles.Any(r => r.RoleName == query.Role));

        if (!string.IsNullOrWhiteSpace(query.Status))
            usersQuery = usersQuery.Where(u => u.Status == query.Status);

        if (query.BranchId.HasValue)
            usersQuery = usersQuery.Where(u => u.DefaultBranchId == query.BranchId);

        var totalCount = await usersQuery.CountAsync();

        var users = await usersQuery
            .OrderBy(u => u.Username)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = users.Select(MapToDto).ToList();

        return PaginatedResponse<SystemUserDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<SystemUserDto> GetUserByIdAsync(Guid userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.DefaultBranch)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        return MapToDto(user);
    }

    public async Task<SystemUserDto> CreateUserAsync(CreateUserRequest request, Guid? currentUserId)
    {
        var username = request.Username.Trim().ToLower();
        var email = request.Email.Trim().ToLower();

        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username))
            throw new BusinessException("Tên đăng nhập đã tồn tại.");

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
            throw new BusinessException("Email đã được sử dụng.");

        var roles = await ResolveRolesAsync(request.Roles);

        if (request.DefaultBranchId.HasValue && !await _context.Branches.AnyAsync(b => b.BranchId == request.DefaultBranchId))
            throw new BusinessException("Chi nhánh không tồn tại.");

        var now = DateTime.UtcNow;

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber?.Trim(),
            AvatarUrl = request.AvatarUrl?.Trim(),
            IsActive = true,
            IsVerified = true,
            Status = DomainConstants.UserStatus.Active,
            DefaultBranchId = request.DefaultBranchId,
            CreatedBy = currentUserId,
            CreatedAt = now,
            UpdatedAt = now,
            Roles = roles
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (user.DefaultBranchId.HasValue)
            await _context.Entry(user).Reference(u => u.DefaultBranch).LoadAsync();

        return MapToDto(user);
    }

    public async Task<SystemUserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .Include(u => u.DefaultBranch)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLower();
            if (await _context.Users.AnyAsync(u => u.UserId != userId && u.Email.ToLower() == email))
                throw new BusinessException("Email đã được sử dụng bởi tài khoản khác.");

            user.Email = email;
        }

        if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName.Trim();
        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber.Trim();
        if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl.Trim();

        if (request.DefaultBranchId.HasValue)
        {
            if (!await _context.Branches.AnyAsync(b => b.BranchId == request.DefaultBranchId))
                throw new BusinessException("Chi nhánh không tồn tại.");

            user.DefaultBranchId = request.DefaultBranchId;
        }

        if (request.Roles != null)
        {
            var roles = await ResolveRolesAsync(request.Roles);
            user.Roles.Clear();
            foreach (var role in roles)
                user.Roles.Add(role);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (user.DefaultBranchId.HasValue && user.DefaultBranch == null)
            await _context.Entry(user).Reference(u => u.DefaultBranch).LoadAsync();

        return MapToDto(user);
    }

    public async Task<SystemUserDto> ChangeStatusAsync(Guid userId, ChangeUserStatusRequest request)
    {
        var validStatuses = new[]
        {
            DomainConstants.UserStatus.Active,
            DomainConstants.UserStatus.Suspended,
            DomainConstants.UserStatus.Banned
        };

        if (!validStatuses.Contains(request.Status))
            throw new BusinessException($"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}.");

        var user = await _context.Users
            .Include(u => u.Roles)
            .Include(u => u.DefaultBranch)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        user.Status = request.Status;
        user.IsActive = request.Status == DomainConstants.UserStatus.Active;
        user.SuspendedUntil = request.Status == DomainConstants.UserStatus.Suspended ? request.SuspendedUntil : null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task ResetPasswordAsync(Guid userId, AdminResetPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        if (user.Roles.Any(r => string.Equals(r.RoleName, DomainConstants.AdminRole, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessException("Không thể xóa tài khoản quản trị hệ thống.");

        var hasActivity = await _context.SystemLogs.AnyAsync(l => l.UserId == userId);
        if (hasActivity)
        {
            // Giữ lại dấu vết nghiệp vụ: xóa mềm
            user.IsActive = false;
            user.Status = DomainConstants.UserStatus.Deleted;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.Users.Remove(user);
        }

        await _context.SaveChangesAsync();
    }

    public async Task AssignPermissionsAsync(Guid userId, AssignUserPermissionsRequest request)
    {
        var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
        if (!userExists) throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        var codes = request.Overrides.Select(o => o.PermissionCode).Distinct().ToList();
        var permissions = await _context.Permissions
            .Where(p => codes.Contains(p.PermissionCode))
            .ToListAsync();

        var missing = codes.Except(permissions.Select(p => p.PermissionCode)).ToList();
        if (missing.Count > 0)
            throw new BusinessException($"Mã quyền không tồn tại: {string.Join(", ", missing)}.");

        var existing = await _context.UserPermissions
            .Where(up => up.UserId == userId)
            .ToListAsync();

        _context.UserPermissions.RemoveRange(existing);

        foreach (var overridden in request.Overrides)
        {
            var permission = permissions.First(p => p.PermissionCode == overridden.PermissionCode);

            _context.UserPermissions.Add(new UserPermission
            {
                UserPermissionId = Guid.NewGuid(),
                UserId = userId,
                PermissionId = permission.PermissionId,
                IsGranted = overridden.IsGranted
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task<List<Role>> ResolveRolesAsync(List<string> roleNames)
    {
        if (roleNames == null || roleNames.Count == 0) return new List<Role>();

        var normalized = roleNames.Select(r => r.Trim()).ToList();

        var roles = await _context.Roles
            .Where(r => normalized.Contains(r.RoleName))
            .ToListAsync();

        var missing = normalized.Except(roles.Select(r => r.RoleName)).ToList();
        if (missing.Count > 0)
            throw new BusinessException($"Role không tồn tại: {string.Join(", ", missing)}.");

        return roles;
    }

    private static SystemUserDto MapToDto(User user) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        IsActive = user.IsActive,
        IsVerified = user.IsVerified,
        Status = user.Status,
        DefaultBranchId = user.DefaultBranchId,
        DefaultBranchName = user.DefaultBranch?.BranchName,
        CreatedAt = user.CreatedAt,
        Roles = user.Roles.Select(r => r.RoleName).ToList()
    };
}
