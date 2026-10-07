using System.Security.Claims;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.AuthDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Services.AuthServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.AuthenticationController;

/// <summary>
/// Thông tin cá nhân của tài khoản đang đăng nhập.
/// </summary>
[ApiController]
[Authorize]
[Route("api/account")]
public class AccountSettingsController : ControllerBase
{
    private readonly IAuthService _authService;

    public AccountSettingsController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Lấy thông tin tài khoản đang đăng nhập (kèm role & permission).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        var profile = await _authService.GetCurrentUserAsync(userId);

        return Ok(profile);
    }

    /// <summary>Cập nhật thông tin cá nhân.</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        var profile = await _authService.UpdateProfileAsync(userId, request);

        return Ok(ApiResponse<UserResponseDto>.Ok(profile, "Cập nhật thông tin cá nhân thành công."));
    }

    /// <summary>Đổi mật khẩu của tài khoản đang đăng nhập.</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        await _authService.ChangePasswordAsync(userId, request);

        return Ok(new { message = "Đổi mật khẩu thành công." });
    }

    private System.Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!System.Guid.TryParse(raw, out var userId))
            throw new UnauthorizedAccessException("Không xác định được người dùng đang đăng nhập.");

        return userId;
    }
}
