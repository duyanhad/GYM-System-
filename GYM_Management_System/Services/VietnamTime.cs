using System;

namespace GYM_Management_System.Services;

/// <summary>
/// Tiện ích chuyển đổi thời gian theo múi giờ Việt Nam (UTC+7).
/// Dữ liệu trong DB luôn lưu ở UTC, chỉ đổi sang giờ VN khi cần tính "hôm nay",
/// "đầu tháng", "cuối ngày" cho báo cáo / thống kê.
/// </summary>
public static class VietnamTime
{
    private static readonly TimeZoneInfo Zone = ResolveTimeZone();

    private static TimeZoneInfo ResolveTimeZone()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.Utc;
    }

    /// <summary>Thời điểm hiện tại theo giờ Việt Nam.</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    /// <summary>Ngày hôm nay (00:00) theo giờ Việt Nam, trả về dạng UTC để so sánh với dữ liệu trong DB.</summary>
    public static DateTime TodayStartUtc => ToUtc(Now.Date);

    /// <summary>Ngày mai (00:00) theo giờ Việt Nam, trả về dạng UTC.</summary>
    public static DateTime TomorrowStartUtc => ToUtc(Now.Date.AddDays(1));

    /// <summary>Ngày đầu tháng hiện tại theo giờ Việt Nam, trả về dạng UTC.</summary>
    public static DateTime MonthStartUtc => ToUtc(new DateTime(Now.Year, Now.Month, 1));

    /// <summary>Ngày đầu tháng trước theo giờ Việt Nam, trả về dạng UTC.</summary>
    public static DateTime PreviousMonthStartUtc => ToUtc(new DateTime(Now.Year, Now.Month, 1).AddMonths(-1));

    /// <summary>Đổi một mốc giờ Việt Nam (Kind = Unspecified) sang UTC.</summary>
    public static DateTime ToUtc(DateTime vietnamLocalTime)
    {
        var unspecified = DateTime.SpecifyKind(vietnamLocalTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, Zone);
    }
}
