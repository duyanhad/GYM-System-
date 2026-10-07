using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GYM_Management_System.DTOs.WorkoutDTOs;

// ============================ BÀI TẬP ============================

public class ExerciseDto
{
    public Guid ExerciseId { get; set; }
    public string Name { get; set; } = "";
    public string MuscleGroup { get; set; } = "";
    public int DefaultSets { get; set; }
    public int DefaultReps { get; set; }
    public int DefaultRestSeconds { get; set; }
    public string? Note { get; set; }

    /// <summary>True = bài tập mẫu của hệ thống (không sửa/xoá được).</summary>
    public bool IsSystem { get; set; }
}

public class CreateExerciseRequest
{
    [Required(ErrorMessage = "Tên bài tập là bắt buộc.")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Nhóm cơ là bắt buộc.")]
    [MaxLength(50)]
    public string MuscleGroup { get; set; } = "";

    [Range(1, 100)] public int DefaultSets { get; set; } = 3;

    [Range(1, 1000)] public int DefaultReps { get; set; } = 10;

    [Range(0, 3600)] public int DefaultRestSeconds { get; set; } = 60;

    [MaxLength(500)] public string? Note { get; set; }
}

public class UpdateExerciseRequest
{
    [MaxLength(200)] public string? Name { get; set; }
    [MaxLength(50)] public string? MuscleGroup { get; set; }
    public int? DefaultSets { get; set; }
    public int? DefaultReps { get; set; }
    public int? DefaultRestSeconds { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
}

// ============================ GIÁO ÁN ============================

public class WorkoutPlanItemDto
{
    public Guid WorkoutPlanItemId { get; set; }
    public int OrderIndex { get; set; }
    public Guid? ExerciseId { get; set; }
    public string ExerciseName { get; set; } = "";
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public int RestSeconds { get; set; }
    public string? Note { get; set; }
}

public class WorkoutPlanDto
{
    public Guid WorkoutPlanId { get; set; }
    public string Name { get; set; } = "";
    public string? Focus { get; set; }
    public string? Note { get; set; }
    public bool IsSystem { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int TotalSets { get; set; }
    public List<WorkoutPlanItemDto> Items { get; set; } = new();
}

public class SaveWorkoutPlanItemRequest
{
    public Guid? ExerciseId { get; set; }

    [Required(ErrorMessage = "Tên bài tập là bắt buộc.")]
    [MaxLength(200)]
    public string ExerciseName { get; set; } = "";

    [Range(1, 100)] public int TargetSets { get; set; } = 3;

    [Range(1, 1000)] public int TargetReps { get; set; } = 10;

    [Range(0, 3600)] public int RestSeconds { get; set; } = 60;

    [MaxLength(500)] public string? Note { get; set; }
}

public class SaveWorkoutPlanRequest
{
    [Required(ErrorMessage = "Tên buổi tập là bắt buộc.")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(200)] public string? Focus { get; set; }

    [MaxLength(1000)] public string? Note { get; set; }

    [MinLength(1, ErrorMessage = "Buổi tập phải có ít nhất 1 bài tập.")]
    public List<SaveWorkoutPlanItemRequest> Items { get; set; } = new();
}

// ============================ LỊCH TUẦN ============================

public class WeeklyScheduleDto
{
    /// <summary>0 = Chủ nhật ... 6 = Thứ bảy.</summary>
    public int DayOfWeek { get; set; }

    public string DayName { get; set; } = "";

    public Guid? WorkoutPlanId { get; set; }

    public string? PlanName { get; set; }

    public string? Focus { get; set; }

    public int ItemsCount { get; set; }
}

public class AssignWeeklyScheduleRequest
{
    [Range(0, 6, ErrorMessage = "Thứ trong tuần phải từ 0 (Chủ nhật) đến 6 (Thứ bảy).")]
    public int DayOfWeek { get; set; }

    /// <summary>Null = nghỉ (không tập).</summary>
    public Guid? WorkoutPlanId { get; set; }
}

// ============================ BUỔI TẬP ============================

public class WorkoutSetLogDto
{
    public Guid WorkoutSetLogId { get; set; }
    public int SetNumber { get; set; }
    public int? Reps { get; set; }
    public int DurationSeconds { get; set; }
    public int RestSeconds { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}

public class SessionExerciseDto
{
    public Guid WorkoutSessionExerciseId { get; set; }
    public int OrderIndex { get; set; }
    public Guid? ExerciseId { get; set; }
    public string ExerciseName { get; set; } = "";
    public string? MuscleGroup { get; set; }
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public int RestSeconds { get; set; }
    public string? Note { get; set; }
    public int TotalWorkSeconds { get; set; }
    public int TotalExerciseRestSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public List<WorkoutSetLogDto> Sets { get; set; } = new();
}

public class WorkoutSessionDto
{
    public Guid WorkoutSessionId { get; set; }
    public DateTime SessionDate { get; set; }
    public string DateKey { get; set; } = "";
    public Guid? WorkoutPlanId { get; set; }
    public string? PlanName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = "";
    public string? Note { get; set; }

    /// <summary>Tổng thời gian tập của cả buổi (giây) - tổng thời gian các hiệp.</summary>
    public int TotalDurationSeconds { get; set; }

    public int TotalRestSeconds { get; set; }
    public int CompletedExercises { get; set; }
    public int TotalExercises { get; set; }
    public int ProgressPercent { get; set; }
    public List<SessionExerciseDto> Exercises { get; set; } = new();
}

public class StartSessionExerciseRequest
{
    public Guid? ExerciseId { get; set; }

    [Required(ErrorMessage = "Tên bài tập là bắt buộc.")]
    [MaxLength(200)]
    public string ExerciseName { get; set; } = "";

    [MaxLength(50)] public string? MuscleGroup { get; set; }

    [Range(1, 100)] public int TargetSets { get; set; } = 3;

    [Range(1, 1000)] public int TargetReps { get; set; } = 10;

    [Range(0, 3600)] public int RestSeconds { get; set; } = 60;

    [MaxLength(500)] public string? Note { get; set; }
}

public class StartSessionRequest
{
    /// <summary>Ngày tập, dạng 'YYYY-MM-DD'.</summary>
    [Required(ErrorMessage = "Ngày tập là bắt buộc.")]
    public string DateKey { get; set; } = "";

    public Guid? WorkoutPlanId { get; set; }

    [MaxLength(200)] public string? PlanName { get; set; }

    /// <summary>Danh sách bài tập theo đúng thứ tự người dùng chọn. Rỗng = lấy theo giáo án của lịch tuần.</summary>
    public List<StartSessionExerciseRequest> Exercises { get; set; } = new();

    /// <summary>Bắt đầu lại từ đầu (xoá các hiệp đã ghi của buổi hôm đó).</summary>
    public bool Restart { get; set; }
}

public class LogSetRequest
{
    [Range(1, 100)] public int SetNumber { get; set; }

    [Range(1, 10000)] public int DurationSeconds { get; set; }

    [Range(0, 3600)] public int RestSeconds { get; set; }

    public int? Reps { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}

public class SetExerciseCompletedRequest
{
    public bool IsCompleted { get; set; }
}

public class ReorderExercisesRequest
{
    [MinLength(1, ErrorMessage = "Cần danh sách thứ tự bài tập.")]
    public List<Guid> OrderedSessionExerciseIds { get; set; } = new();
}

public class FinishSessionRequest
{
    [MaxLength(1000)] public string? Note { get; set; }
}

public class WorkoutHistoryQueryParameters
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class WorkoutStatsDto
{
    public int StreakDays { get; set; }
    public int SessionsThisMonth { get; set; }
    public int TotalSessions { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int ScheduledWeekdays { get; set; }
    public int CompletionRate { get; set; }
}

public class WorkoutDayMarkDto
{
    public string DateKey { get; set; } = "";
    public bool HasSession { get; set; }
    public bool IsCompleted { get; set; }
    public int TotalDurationSeconds { get; set; }
    public int CompletedExercises { get; set; }
    public int TotalExercises { get; set; }
}

// ============================ KẾT NỐI & CHIA SẺ ============================

public class ConnectionDto
{
    public Guid UserConnectionId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Status { get; set; } = "";

    /// <summary>SENT = mình gửi lời mời, RECEIVED = người khác mời mình.</summary>
    public string Direction { get; set; } = "";
}

public class InviteConnectionRequest
{
    [Required(ErrorMessage = "Nhập tên đăng nhập hoặc email.")]
    [MaxLength(255)]
    public string Query { get; set; } = "";
}

public class ShareWorkoutPlanRequest
{
    [Required] public Guid WorkoutPlanId { get; set; }

    public Guid? ToUserId { get; set; }

    public Guid? UserConnectionId { get; set; }

    [MaxLength(500)] public string? Message { get; set; }
}

public class WorkoutPlanShareDto
{
    public Guid WorkoutPlanShareId { get; set; }
    public Guid WorkoutPlanId { get; set; }
    public string PlanName { get; set; } = "";
    public Guid FromUserId { get; set; }
    public string FromUserName { get; set; } = "";
    public Guid ToUserId { get; set; }
    public string ToUserName { get; set; } = "";
    public string? Message { get; set; }
    public DateTime SharedAt { get; set; }
    public bool IsReceived { get; set; }
}

public class UserNotificationDto
{
    public Guid UserNotificationId { get; set; }
    public string Title { get; set; } = "";
    public string? Message { get; set; }
    public string Type { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
