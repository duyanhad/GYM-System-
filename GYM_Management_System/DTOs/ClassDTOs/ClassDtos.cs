using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GYM_Management_System.DTOs.ClassDTOs;

public class TrainingClassDto
{
    public Guid ClassId { get; set; }
    public string ClassCode { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string? Description { get; set; }
    public string Level { get; set; } = "";
    public Guid? TrainerId { get; set; }
    public string? TrainerName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int Capacity { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PricePerSession { get; set; }
    public bool IsActive { get; set; }
    public int SchedulesCount { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateTrainingClassRequest
{
    [Required(ErrorMessage = "Mã lớp là bắt buộc.")]
    [MaxLength(50)]
    public string ClassCode { get; set; } = "";

    [Required(ErrorMessage = "Tên lớp là bắt buộc.")]
    [MaxLength(200)]
    public string ClassName { get; set; } = "";

    public string? Description { get; set; }

    /// <summary>BEGINNER / INTERMEDIATE / ADVANCED.</summary>
    public string Level { get; set; } = Models.DomainConstants.ClassLevel.Beginner;

    public Guid? TrainerId { get; set; }
    public Guid? BranchId { get; set; }

    [Range(1, 500, ErrorMessage = "Sức chứa lớp phải từ 1 đến 500.")]
    public int Capacity { get; set; } = 20;

    [Range(15, 300, ErrorMessage = "Thời lượng buổi học phải từ 15 đến 300 phút.")]
    public int DurationMinutes { get; set; } = 60;

    public decimal PricePerSession { get; set; }
}

public class UpdateTrainingClassRequest
{
    [MaxLength(200)]
    public string? ClassName { get; set; }

    public string? Description { get; set; }
    public string? Level { get; set; }
    public Guid? TrainerId { get; set; }
    public Guid? BranchId { get; set; }
    public int? Capacity { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal? PricePerSession { get; set; }
    public bool? IsActive { get; set; }
}

public class TrainingClassQueryParameters
{
    public string? Search { get; set; }
    public string? Level { get; set; }
    public Guid? TrainerId { get; set; }
    public Guid? BranchId { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ClassScheduleDto
{
    public Guid ScheduleId { get; set; }
    public Guid ClassId { get; set; }
    public string? ClassName { get; set; }
    public Guid? TrainerId { get; set; }
    public string? TrainerName { get; set; }
    public string? Room { get; set; }
    public int DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; } = "";
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public int BookedCount { get; set; }
}

public class CreateClassScheduleRequest
{
    [Required(ErrorMessage = "Lớp học là bắt buộc.")]
    public Guid ClassId { get; set; }

    public Guid? TrainerId { get; set; }
    public string? Room { get; set; }

    /// <summary>0 = Chủ nhật ... 6 = Thứ bảy.</summary>
    [Range(0, 6, ErrorMessage = "Thứ trong tuần phải từ 0 (Chủ nhật) đến 6 (Thứ bảy).")]
    public int DayOfWeek { get; set; }

    [Required(ErrorMessage = "Giờ bắt đầu là bắt buộc.")]
    public TimeSpan StartTime { get; set; }

    [Required(ErrorMessage = "Giờ kết thúc là bắt buộc.")]
    public TimeSpan EndTime { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class UpdateClassScheduleRequest
{
    public Guid? TrainerId { get; set; }
    public string? Room { get; set; }
    public int? DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool? IsActive { get; set; }
}

public class ClassBookingDto
{
    public Guid BookingId { get; set; }
    public Guid ClassId { get; set; }
    public string? ClassName { get; set; }
    public Guid? ScheduleId { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberCode { get; set; }
    public DateTime BookedAt { get; set; }
    public string Status { get; set; } = "";
    public DateTime? CancelledAt { get; set; }
    public string? Notes { get; set; }
}

public class CreateBookingRequest
{
    [Required(ErrorMessage = "Lớp học là bắt buộc.")]
    public Guid ClassId { get; set; }

    [Required(ErrorMessage = "Hội viên là bắt buộc.")]
    public Guid MemberId { get; set; }

    public Guid? ScheduleId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateBookingStatusRequest
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
    public string Status { get; set; } = "";
}

public class ClassBookingQueryParameters
{
    public Guid? ClassId { get; set; }
    public Guid? MemberId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Lịch học trong tuần (dùng cho màn hình thời khóa biểu).</summary>
public class WeeklyScheduleDto
{
    public int DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; } = "";
    public List<ClassScheduleDto> Schedules { get; set; } = new();
}
