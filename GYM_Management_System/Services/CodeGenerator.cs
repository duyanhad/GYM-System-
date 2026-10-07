using System;
using System.Globalization;
using System.Text;

namespace GYM_Management_System.Services;

/// <summary>
/// Sinh mã nghiệp vụ tự động: MEM0001, SUB20261008-0007, INV..., PAY...
/// </summary>
public static class CodeGenerator
{
    public static string MemberCode(int sequence) => $"MEM{sequence:D5}";

    public static string TrainerCode(int sequence) => $"PT{sequence:D4}";

    public static string SubscriptionCode(DateTime date, int sequence)
        => $"SUB{date:yyyyMMdd}-{sequence:D4}";

    public static string InvoiceCode(DateTime date, int sequence)
        => $"INV{date:yyyyMMdd}-{sequence:D4}";

    public static string PaymentCode(DateTime date, int sequence)
        => $"PAY{date:yyyyMMdd}-{sequence:D4}";

    public static string BranchCode(int sequence) => $"BR{sequence:D3}";

    public static string PlanCode(int sequence) => $"PLAN{sequence:D3}";

    public static string ClassCode(int sequence) => $"CLS{sequence:D3}";

    /// <summary>Mã OTP gồm 6 chữ số.</summary>
    public static string OtpCode()
    {
        var value = Random.Shared.Next(0, 1_000_000);
        return value.ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <summary>Token ngẫu nhiên dùng cho link đặt lại mật khẩu.</summary>
    public static string SecureToken()
    {
        var bytes = new byte[32];
        Random.Shared.NextBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <summary>Tạo slug không dấu (dùng cho tên file / mã lớp).</summary>
    public static string Slug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
