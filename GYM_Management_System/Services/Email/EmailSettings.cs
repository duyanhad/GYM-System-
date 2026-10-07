namespace GYM_Management_System.Services.Email;

/// <summary>
/// Cấu hình SMTP đọc từ appsettings.json (section "EmailSettings").
/// </summary>
public class EmailSettings
{
    public string SmtpServer { get; set; } = "";
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = "";
    public string SenderEmail { get; set; } = "";
    public string Password { get; set; } = "";
}
