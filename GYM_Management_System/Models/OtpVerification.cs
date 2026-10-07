using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Mã OTP dùng cho đăng ký / quên mật khẩu.
/// </summary>
public partial class OtpVerification
{
    public Guid OtpId { get; set; }

    public string Email { get; set; } = "";

    public string OtpCode { get; set; } = "";

    /// <summary>REGISTER / FORGOT_PASSWORD.</summary>
    public string Purpose { get; set; } = "FORGOT_PASSWORD";

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; }
}
