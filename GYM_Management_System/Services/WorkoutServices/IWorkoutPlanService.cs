using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.WorkoutDTOs;

namespace GYM_Management_System.Services.WorkoutServices;

public interface IWorkoutPlanService
{
    Task<List<WorkoutPlanDto>> GetPlansAsync(Guid userId);

    Task<WorkoutPlanDto> GetPlanAsync(Guid userId, Guid planId);

    Task<WorkoutPlanDto> SavePlanAsync(Guid userId, SaveWorkoutPlanRequest request, Guid? planId = null);

    Task DeletePlanAsync(Guid userId, Guid planId);

    Task<List<WeeklyScheduleDto>> GetWeeklyScheduleAsync(Guid userId);

    Task<WeeklyScheduleDto> AssignWeeklyScheduleAsync(Guid userId, AssignWeeklyScheduleRequest request);

    /// <summary>Bài tập cần tập của một ngày theo lịch tuần (kèm buổi liền trước nếu sát nhau).</summary>
    Task<List<SaveWorkoutPlanItemRequest>> ComposeItemsForDateAsync(Guid userId, DateTime date);

    /// <summary>Giáo án đang gán cho ngày đó (nếu có).</summary>
    Task<WorkoutPlanDto?> GetPlanForDateAsync(Guid userId, DateTime date);
}
