using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.AdminDTOs;

namespace GYM_Management_System.Services.AdminServices;

public interface IAdminUserService
{
    Task<PaginatedResponse<SystemUserDto>> GetUsersAsync(UserQueryParameters query);

    Task<SystemUserDto> GetUserByIdAsync(Guid userId);

    Task<SystemUserDto> CreateUserAsync(CreateUserRequest request, Guid? currentUserId);

    Task<SystemUserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request);

    Task<SystemUserDto> ChangeStatusAsync(Guid userId, ChangeUserStatusRequest request);

    Task ResetPasswordAsync(Guid userId, AdminResetPasswordRequest request);

    Task DeleteUserAsync(Guid userId);

    Task AssignPermissionsAsync(Guid userId, AssignUserPermissionsRequest request);
}
