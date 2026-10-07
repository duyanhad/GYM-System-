using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Token đặt lại mật khẩu (gửi qua email dưới dạng link).
/// </summary>
public partial class PasswordResetToken
{
    public Guid TokenId { get; set; }

    public Guid UserId { get; set; }

    public string Token { get; set; } = "";

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
