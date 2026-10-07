using System;

namespace GYM_Management_System.Models;

/// <summary>
/// Lịch tập tuần của người dùng: mỗi thứ (0 = Chủ nhật ... 6 = Thứ bảy) gắn 1 giáo án hoặc nghỉ.
/// </summary>
public partial class WeeklyScheduleEntry
{
    public Guid WeeklyScheduleEntryId { get; set; }

    public Guid UserId { get; set; }

    public int DayOfWeek { get; set; }

    public Guid? WorkoutPlanId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
