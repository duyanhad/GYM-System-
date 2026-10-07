using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GYM_Management_System.Services.Email;

public interface IEmailService
{
    Task SendOtpAsync(string toEmail, string otpCode, string purpose);

    Task SendPasswordResetLinkAsync(string toEmail, string resetLink);
}

/// <summary>
/// Gửi email qua SMTP (MailKit). Nếu chưa cấu hình SMTP thì chỉ ghi log
/// để môi trường dev vẫn chạy được.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    private bool IsConfigured
        => !string.IsNullOrWhiteSpace(_settings.SmtpServer)
           && !string.IsNullOrWhiteSpace(_settings.SenderEmail)
           && !string.IsNullOrWhiteSpace(_settings.Password);

    public Task SendOtpAsync(string toEmail, string otpCode, string purpose)
    {
        var subject = purpose == "REGISTER" ? "Mã xác thực đăng ký tài khoản" : "Mã xác thực đặt lại mật khẩu";

        var body = $"""
            <p>Xin chào,</p>
            <p>Mã xác thực (OTP) của bạn là:</p>
            <h2 style="letter-spacing:4px">{otpCode}</h2>
            <p>Mã có hiệu lực trong 10 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
            <p>Trân trọng,<br/>GYM SYSTEM</p>
            """;

        return SendEmailAsync(toEmail, subject, body);
    }

    public Task SendPasswordResetLinkAsync(string toEmail, string resetLink)
    {
        var body = $"""
            <p>Xin chào,</p>
            <p>Bạn (hoặc ai đó) đã yêu cầu đặt lại mật khẩu cho tài khoản GYM SYSTEM.</p>
            <p><a href="{resetLink}">Bấm vào đây để đặt lại mật khẩu</a></p>
            <p>Nếu không phải bạn, hãy bỏ qua email này.</p>
            """;

        return SendEmailAsync(toEmail, "Đặt lại mật khẩu GYM SYSTEM", body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning(
                "EmailSettings chưa được cấu hình. Bỏ qua việc gửi email '{Subject}' tới {Email}.",
                subject, toEmail);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.SenderEmail, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
