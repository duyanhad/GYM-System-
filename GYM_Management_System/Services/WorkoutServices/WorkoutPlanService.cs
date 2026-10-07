using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.WorkoutDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.WorkoutServices;

/// <summary>
/// Giáo án (buổi tập) + lịch tập tuần của người dùng.
/// </summary>
public class WorkoutPlanService : IWorkoutPlanService
{
    private static readonly string[] DayNames =
    {
        "Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy"
    };

    private readonly GymDbContext _context;

    public WorkoutPlanService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkoutPlanDto>> GetPlansAsync(Guid userId)
    {
        var plans = await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.IsActive && (p.UserId == null || p.UserId == userId))
            .OrderBy(p => p.UserId == null ? 1 : 0)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return plans.Select(MapToDto).ToList();
    }

    public async Task<WorkoutPlanDto> GetPlanAsync(Guid userId, Guid planId)
    {
        var plan = await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.WorkoutPlanId == planId && (p.UserId == null || p.UserId == userId))
            ?? throw new KeyNotFoundException("Không tìm thấy buổi tập.");

        return MapToDto(plan);
    }

    public async Task<WorkoutPlanDto> SavePlanAsync(Guid userId, SaveWorkoutPlanRequest request, Guid? planId = null)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new BusinessException("Buổi tập phải có ít nhất 1 bài tập.");

        var now = DateTime.UtcNow;
        WorkoutPlan plan;

        if (planId.HasValue)
        {
            plan = await _context.WorkoutPlans
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.WorkoutPlanId == planId)
                ?? throw new KeyNotFoundException("Không tìm thấy buổi tập.");

            if (plan.UserId != userId)
                throw new BusinessException("Chỉ có thể sửa buổi tập do bạn tạo.");

            _context.WorkoutPlanItems.RemoveRange(plan.Items);
            plan.Items.Clear();
        }
        else
        {
            plan = new WorkoutPlan
            {
                WorkoutPlanId = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = now
            };

            _context.WorkoutPlans.Add(plan);
        }

        plan.Name = request.Name.Trim();
        plan.Focus = request.Focus?.Trim();
        plan.Note = request.Note?.Trim();
        plan.IsActive = true;
        plan.UpdatedAt = now;

        var order = 1;

        foreach (var item in request.Items)
        {
            plan.Items.Add(new WorkoutPlanItem
            {
                WorkoutPlanItemId = Guid.NewGuid(),
                WorkoutPlanId = plan.WorkoutPlanId,
                OrderIndex = order++,
                ExerciseId = item.ExerciseId,
                ExerciseName = item.ExerciseName.Trim(),
                TargetSets = item.TargetSets,
                TargetReps = item.TargetReps,
                RestSeconds = item.RestSeconds,
                Note = item.Note?.Trim()
            });
        }

        await _context.SaveChangesAsync();

        return MapToDto(plan);
    }

    public async Task DeletePlanAsync(Guid userId, Guid planId)
    {
        var plan = await _context.WorkoutPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.WorkoutPlanId == planId)
            ?? throw new KeyNotFoundException("Không tìm thấy buổi tập.");

        if (plan.UserId != userId)
            throw new BusinessException("Chỉ có thể xoá buổi tập do bạn tạo.");

        var scheduleEntries = await _context.WeeklyScheduleEntries
            .Where(e => e.UserId == userId && e.WorkoutPlanId == planId)
            .ToListAsync();

        foreach (var entry in scheduleEntries)
            entry.WorkoutPlanId = null;

        _context.WorkoutPlans.Remove(plan);
        await _context.SaveChangesAsync();
    }

    public async Task<List<WeeklyScheduleDto>> GetWeeklyScheduleAsync(Guid userId)
    {
        var entries = await _context.WeeklyScheduleEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .ToListAsync();

        var planIds = entries.Where(e => e.WorkoutPlanId.HasValue).Select(e => e.WorkoutPlanId!.Value).ToList();

        var plans = await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => planIds.Contains(p.WorkoutPlanId))
            .ToListAsync();

        return Enumerable.Range(0, 7)
            .Select(day =>
            {
                var entry = entries.FirstOrDefault(e => e.DayOfWeek == day);
                var plan = entry?.WorkoutPlanId.HasValue == true
                    ? plans.FirstOrDefault(p => p.WorkoutPlanId == entry.WorkoutPlanId)
                    : null;

                return new WeeklyScheduleDto
                {
                    DayOfWeek = day,
                    DayName = DayNames[day],
                    WorkoutPlanId = plan?.WorkoutPlanId,
                    PlanName = plan?.Name,
                    Focus = plan?.Focus,
                    ItemsCount = plan?.Items.Count ?? 0
                };
            })
            .ToList();
    }

    public async Task<WeeklyScheduleDto> AssignWeeklyScheduleAsync(Guid userId, AssignWeeklyScheduleRequest request)
    {
        if (request.DayOfWeek is < 0 or > 6)
            throw new BusinessException("Thứ trong tuần không hợp lệ.");

        if (request.WorkoutPlanId.HasValue)
        {
            var planExists = await _context.WorkoutPlans
                .AnyAsync(p => p.WorkoutPlanId == request.WorkoutPlanId && (p.UserId == null || p.UserId == userId));

            if (!planExists) throw new KeyNotFoundException("Không tìm thấy buổi tập.");
        }

        var entry = await _context.WeeklyScheduleEntries
            .FirstOrDefaultAsync(e => e.UserId == userId && e.DayOfWeek == request.DayOfWeek);

        if (entry == null)
        {
            entry = new WeeklyScheduleEntry
            {
                WeeklyScheduleEntryId = Guid.NewGuid(),
                UserId = userId,
                DayOfWeek = request.DayOfWeek,
                CreatedAt = DateTime.UtcNow
            };

            _context.WeeklyScheduleEntries.Add(entry);
        }

        entry.WorkoutPlanId = request.WorkoutPlanId;
        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var schedule = await GetWeeklyScheduleAsync(userId);

        return schedule.First(item => item.DayOfWeek == request.DayOfWeek);
    }

    public async Task<WorkoutPlanDto?> GetPlanForDateAsync(Guid userId, DateTime date)
    {
        var weekday = (int)date.DayOfWeek;

        var entry = await _context.WeeklyScheduleEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.DayOfWeek == weekday);

        if (entry?.WorkoutPlanId == null) return null;

        var plan = await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.WorkoutPlanId == entry.WorkoutPlanId);

        return plan == null ? null : MapToDto(plan);
    }

    /// <summary>
    /// Bài tập cần tập của một ngày = các bài trong giáo án đang gán cho thứ đó (theo đúng thứ tự).
    /// Ngày không xếp buổi tập thì trả về danh sách rỗng (FE sẽ cho người dùng tự chọn bài).
    /// </summary>
    public async Task<List<SaveWorkoutPlanItemRequest>> ComposeItemsForDateAsync(Guid userId, DateTime date)
    {
        var plan = await GetPlanForDateAsync(userId, date);

        if (plan == null) return new List<SaveWorkoutPlanItemRequest>();

        return plan.Items
            .OrderBy(item => item.OrderIndex)
            .Select(item => new SaveWorkoutPlanItemRequest
            {
                ExerciseId = item.ExerciseId,
                ExerciseName = item.ExerciseName,
                TargetSets = item.TargetSets,
                TargetReps = item.TargetReps,
                RestSeconds = item.RestSeconds,
                Note = item.Note
            })
            .ToList();
    }

    private static WorkoutPlanDto MapToDto(WorkoutPlan plan) => new()
    {
        WorkoutPlanId = plan.WorkoutPlanId,
        Name = plan.Name,
        Focus = plan.Focus,
        Note = plan.Note,
        IsSystem = plan.UserId == null,
        CreatedAt = plan.CreatedAt,
        TotalSets = plan.Items.Sum(item => item.TargetSets),
        Items = plan.Items
            .OrderBy(item => item.OrderIndex)
            .Select(item => new WorkoutPlanItemDto
            {
                WorkoutPlanItemId = item.WorkoutPlanItemId,
                OrderIndex = item.OrderIndex,
                ExerciseId = item.ExerciseId,
                ExerciseName = item.ExerciseName,
                TargetSets = item.TargetSets,
                TargetReps = item.TargetReps,
                RestSeconds = item.RestSeconds,
                Note = item.Note
            })
            .ToList()
    };
}
