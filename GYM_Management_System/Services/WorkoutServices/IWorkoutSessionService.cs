using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.WorkoutDTOs;

namespace GYM_Management_System.Services.WorkoutServices;

public interface IWorkoutSessionService
{
    /// <summary>Bắt đầu (hoặc lấy lại) buổi tập của một ngày.</summary>
    Task<WorkoutSessionDto> StartSessionAsync(Guid userId, StartSessionRequest request);

    Task<WorkoutSessionDto?> GetSessionByDateAsync(Guid userId, string dateKey);

    Task<WorkoutSessionDto> GetSessionByIdAsync(Guid userId, Guid sessionId);

    /// <summary>Ghi lại một hiệp: thời gian tập + thời gian nghỉ (do người dùng bấm).</summary>
    Task<WorkoutSessionDto> LogSetAsync(Guid userId, Guid sessionId, Guid sessionExerciseId, LogSetRequest request);

    Task<WorkoutSessionDto> SetExerciseCompletedAsync(Guid userId, Guid sessionExerciseId, bool isCompleted);

    Task<WorkoutSessionDto> ReorderExercisesAsync(Guid userId, Guid sessionId, ReorderExercisesRequest request);

    Task<WorkoutSessionDto> FinishSessionAsync(Guid userId, Guid sessionId, FinishSessionRequest request);

    Task DeleteSessionAsync(Guid userId, Guid sessionId);

    Task<PaginatedResponse<WorkoutSessionDto>> GetHistoryAsync(Guid userId, WorkoutHistoryQueryParameters query);

    /// <summary>Đánh dấu các ngày đã tập trong khoảng (dùng cho lịch tháng).</summary>
    Task<List<WorkoutDayMarkDto>> GetDayMarksAsync(Guid userId, DateTime fromDate, DateTime toDate);

    Task<WorkoutStatsDto> GetStatsAsync(Guid userId);
}
