using System;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.AuthDTOs;

namespace GYM_Management_System.Services.AuthServices;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginDto dto);

    Task<UserResponseDto> GetCurrentUserAsync(Guid userId);

    Task<UserResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);

    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

    Task SendForgotPasswordOtpAsync(ForgotPasswordStartDto dto);

    Task ResetPasswordWithOtpAsync(ForgotPasswordVerifyDto dto);
}
