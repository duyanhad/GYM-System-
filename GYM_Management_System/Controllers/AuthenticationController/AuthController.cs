using System.Threading.Tasks;
using GYM_Management_System.DTOs.AuthDTOs;
using GYM_Management_System.Services.AuthServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GYM_Management_System.Controllers.AuthenticationController;

/// <summary>
/// Xác thực người dùng: đăng nhập, quên mật khẩu.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Đăng nhập và nhận JWT token.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("StrictLimit")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(result);
    }

    /// <summary>Gửi mã OTP đặt lại mật khẩu tới email.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("StrictLimit")]
    [HttpPost("forgot-password/start")]
    public async Task<IActionResult> ForgotPasswordStart([FromBody] ForgotPasswordStartDto dto)
    {
        await _authService.SendForgotPasswordOtpAsync(dto);
        return Ok(new { message = "Nếu email tồn tại trong hệ thống, mã xác thực đã được gửi đi." });
    }

    /// <summary>Xác thực OTP và đặt lại mật khẩu mới.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("StrictLimit")]
    [HttpPost("forgot-password/verify")]
    public async Task<IActionResult> ForgotPasswordVerify([FromBody] ForgotPasswordVerifyDto dto)
    {
        await _authService.ResetPasswordWithOtpAsync(dto);
        return Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập với mật khẩu mới." });
    }
}
