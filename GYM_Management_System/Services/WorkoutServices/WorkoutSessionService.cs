using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.WorkoutDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.WorkoutServices;

/// <summary>
/// Buổi tập của người dùng: chọn bài + thứ tự, ghi thời gian từng hiệp và giờ nghỉ,
/// tổng thời gian tập của cả ngày.
/// </summary>
public class WorkoutSessionService : IWorkoutSessionService
{
    private readonly GymDbContext _context;
    private readonly IWorkoutPlanService _planService;

    public WorkoutSessionService(GymDbContext context, IWorkoutPlanService planService)
    {
        _context = context;
        _planService = planService;
    }

    public async Task<WorkoutSessionDto> StartSessionAsync(Guid userId, StartSessionRequest request)
    {
        var sessionDate = ParseDateKey(request.DateKey);

        var existing = await LoadSessionAsync(userId, sessionDate);

        if (existing != null && !request.Restart)
            return MapToDto(existing);

        if (existing != null)
        {
            _context.WorkoutSessions.Remove(existing);
            await _context.SaveChangesAsync();
        }

        var items = request.Exercises.Count > 0
            ? request.Exercises.Select(item => new SaveWorkoutPlanItemRequest
            {
                ExerciseId = item.ExerciseId,
                ExerciseName = item.ExerciseName,
                TargetSets = item.TargetSets,
                TargetReps = item.TargetReps,
                RestSeconds = item.RestSeconds,
                Note = item.Note
            }).ToList()
            : await _planService.ComposeItemsForDateAsync(userId, sessionDate);

        if (items.Count == 0)
            throw new BusinessException("Chưa có bài tập nào cho buổi tập. Hãy chọn bài tập hoặc xếp lịch tuần trước.");

        var planForDate = await _planService.GetPlanForDateAsync(userId, sessionDate);
        var now = DateTime.UtcNow;

        var session = new WorkoutSession
        {
            WorkoutSessionId = Guid.NewGuid(),
            UserId = userId,
            SessionDate = sessionDate,
            WorkoutPlanId = request.WorkoutPlanId ?? planForDate?.WorkoutPlanId,
            PlanName = request.PlanName ?? planForDate?.Name,
            StartedAt = now,
            Status = DomainConstants.WorkoutSessionStatus.InProgress,
            CreatedAt = now,
            UpdatedAt = now
        };

        var order = 1;

        foreach (var item in items)
        {
            var exercise = await _context.Exercises
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ExerciseId == item.ExerciseId);

            session.Exercises.Add(new WorkoutSessionExercise
            {
                WorkoutSessionExerciseId = Guid.NewGuid(),
                WorkoutSessionId = session.WorkoutSessionId,
                OrderIndex = order++,
                ExerciseId = item.ExerciseId,
                ExerciseName = item.ExerciseName.Trim(),
                MuscleGroup = exercise?.MuscleGroup,
                TargetSets = item.TargetSets,
                TargetReps = item.TargetReps,
                RestSeconds = item.RestSeconds,
                Note = item.Note
            });
        }

        _context.WorkoutSessions.Add(session);
        await _context.SaveChangesAsync();

        return await GetSessionByIdAsync(userId, session.WorkoutSessionId);
    }

    public async Task<WorkoutSessionDto?> GetSessionByDateAsync(Guid userId, string dateKey)
    {
        var session = await LoadSessionAsync(userId, ParseDateKey(dateKey));

        return session == null ? null : MapToDto(session);
    }

    public async Task<WorkoutSessionDto> GetSessionByIdAsync(Guid userId, Guid sessionId)
    {
        var session = await _context.WorkoutSessions
            .AsNoTracking()
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.WorkoutSessionId == sessionId && s.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy buổi tập.");

        return MapToDto(session);
    }

    public async Task<WorkoutSessionDto> LogSetAsync(Guid userId, Guid sessionId, Guid sessionExerciseId, LogSetRequest request)
    {
        var session = await _context.WorkoutSessions
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.WorkoutSessionId == sessionId && s.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy buổi tập.");

        if (session.Status == DomainConstants.WorkoutSessionStatus.Finished)
            throw new BusinessException("Buổi tập đã kết thúc, không thể ghi thêm hiệp.");

        var exercise = session.Exercises.FirstOrDefault(e => e.WorkoutSessionExerciseId == sessionExerciseId)
            ?? throw new KeyNotFoundException("Không tìm thấy bài tập trong buổi.");

        var setLog = exercise.Sets.FirstOrDefault(s => s.SetNumber == request.SetNumber);

        if (setLog == null)
        {
            setLog = new WorkoutSetLog
            {
                WorkoutSetLogId = Guid.NewGuid(),
                WorkoutSessionExerciseId = exercise.WorkoutSessionExerciseId,
                SetNumber = request.SetNumber
            };

            // Thêm tường minh vào DbSet để EF đánh dấu Added (thêm vào collection của entity đang
            // được track sẽ bị EF coi là Modified vì khoá đã có giá trị, gây lỗi UPDATE 0 dòng).
            // EF sẽ tự fixup vào exercise.Sets, không thêm vào collection lần nữa để tránh trùng phần tử.
            _context.WorkoutSetLogs.Add(setLog);
        }

        setLog.DurationSeconds = request.DurationSeconds;
        setLog.RestSeconds = request.RestSeconds;
        setLog.Reps = request.Reps;
        setLog.StartedAt = request.StartedAt ?? setLog.StartedAt;
        setLog.FinishedAt = request.FinishedAt ?? DateTime.UtcNow;
        setLog.Status = DomainConstants.WorkoutSessionStatus.Done;

        RecalculateTotals(session);
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetSessionByIdAsync(userId, sessionId);
    }

    public async Task<WorkoutSessionDto> SetExerciseCompletedAsync(Guid userId, Guid sessionExerciseId, bool isCompleted)
    {
        var exercise = await _context.WorkoutSessionExercises
            .Include(e => e.Sets)
            .Include(e => e.WorkoutSession).ThenInclude(s => s.Exercises)
            .FirstOrDefaultAsync(e => e.WorkoutSessionExerciseId == sessionExerciseId)
            ?? throw new KeyNotFoundException("Không tìm thấy bài tập trong buổi.");

        if (exercise.WorkoutSession.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền cập nhật buổi tập này.");

        exercise.IsCompleted = isCompleted;

        var session = exercise.WorkoutSession;
        RecalculateTotals(session);

        if (session.Exercises.Count > 0 && session.Exercises.All(e => e.IsCompleted))
        {
            session.Status = DomainConstants.WorkoutSessionStatus.Finished;
            session.FinishedAt ??= DateTime.UtcNow;
        }
        else if (session.Status == DomainConstants.WorkoutSessionStatus.Finished)
        {
            session.Status = DomainConstants.WorkoutSessionStatus.InProgress;
            session.FinishedAt = null;
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetSessionByIdAsync(userId, session.WorkoutSessionId);
    }

    public async Task<WorkoutSessionDto> ReorderExercisesAsync(Guid userId, Guid sessionId, ReorderExercisesRequest request)
    {
        var session = await _context.WorkoutSessions
            .Include(s => s.Exercises)
            .FirstOrDefaultAsync(s => s.WorkoutSessionId == sessionId && s.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy buổi tập.");

        var order = 1;

        foreach (var id in request.OrderedSessionExerciseIds)
        {
            var exercise = session.Exercises.FirstOrDefault(e => e.WorkoutSessionExerciseId == id);
            if (exercise == null) continue;

            exercise.OrderIndex = order++;
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetSessionByIdAsync(userId, sessionId);
    }

    public async Task<WorkoutSessionDto> FinishSessionAsync(Guid userId, Guid sessionId, FinishSessionRequest request)
    {
        var session = await _context.WorkoutSessions
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.WorkoutSessionId == sessionId && s.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy buổi tập.");

        RecalculateTotals(session);

        session.Status = DomainConstants.WorkoutSessionStatus.Finished;
        session.FinishedAt = DateTime.UtcNow;
        session.Note = request.Note ?? session.Note;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetSessionByIdAsync(userId, sessionId);
    }

    public async Task DeleteSessionAsync(Guid userId, Guid sessionId)
    {
        var session = await _context.WorkoutSessions
            .FirstOrDefaultAsync(s => s.WorkoutSessionId == sessionId && s.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy buổi tập.");

        _context.WorkoutSessions.Remove(session);
        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<WorkoutSessionDto>> GetHistoryAsync(Guid userId, WorkoutHistoryQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var sessionsQuery = _context.WorkoutSessions
            .AsNoTracking()
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .Where(s => s.UserId == userId);

        if (query.FromDate.HasValue) sessionsQuery = sessionsQuery.Where(s => s.SessionDate >= query.FromDate.Value.Date);
        if (query.ToDate.HasValue) sessionsQuery = sessionsQuery.Where(s => s.SessionDate <= query.ToDate.Value.Date);

        var totalCount = await sessionsQuery.CountAsync();

        var sessions = await sessionsQuery
            .OrderByDescending(s => s.SessionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PaginatedResponse<WorkoutSessionDto>.Create(
            sessions.Select(MapToDto).ToList(), totalCount, pageNumber, pageSize);
    }

    public async Task<List<WorkoutDayMarkDto>> GetDayMarksAsync(Guid userId, DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;

        var sessions = await _context.WorkoutSessions
            .AsNoTracking()
            .Include(s => s.Exercises)
            .Where(s => s.UserId == userId && s.SessionDate >= from && s.SessionDate <= to)
            .ToListAsync();

        return sessions.Select(session => new WorkoutDayMarkDto
        {
            DateKey = session.SessionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            HasSession = true,
            IsCompleted = session.Status == DomainConstants.WorkoutSessionStatus.Finished,
            TotalDurationSeconds = session.TotalDurationSeconds,
            CompletedExercises = session.Exercises.Count(e => e.IsCompleted),
            TotalExercises = session.Exercises.Count
        }).ToList();
    }

    public async Task<WorkoutStatsDto> GetStatsAsync(Guid userId)
    {
        var sessions = await _context.WorkoutSessions
            .AsNoTracking()
            .Include(s => s.Exercises)
            .Where(s => s.UserId == userId)
            .ToListAsync();

        // Khoá ngày của tính năng tập luyện là ngày theo giờ địa phương của người tập
        // (client gửi 'yyyy-MM-dd'), nên "hôm nay" phải tính theo giờ địa phương, không phải UTC.
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var trainedDays = sessions.Select(s => s.SessionDate.Date).Distinct().ToHashSet();

        var streak = 0;
        var cursor = trainedDays.Contains(today) ? today : today.AddDays(-1);

        while (trainedDays.Contains(cursor))
        {
            streak += 1;
            cursor = cursor.AddDays(-1);
        }

        var finishedSessions = sessions.Count(s => s.Status == DomainConstants.WorkoutSessionStatus.Finished);

        var scheduledWeekdays = await _context.WeeklyScheduleEntries
            .CountAsync(e => e.UserId == userId && e.WorkoutPlanId != null);

        return new WorkoutStatsDto
        {
            StreakDays = streak,
            SessionsThisMonth = trainedDays.Count(day => day >= monthStart),
            TotalSessions = sessions.Count,
            TotalDurationMinutes = sessions.Sum(s => s.TotalDurationSeconds) / 60,
            ScheduledWeekdays = scheduledWeekdays,
            CompletionRate = sessions.Count == 0 ? 0 : (int)Math.Round(finishedSessions * 100.0 / sessions.Count)
        };
    }

    private async Task<WorkoutSession?> LoadSessionAsync(Guid userId, DateTime sessionDate)
        => await _context.WorkoutSessions
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionDate == sessionDate);

    private static DateTime ParseDateKey(string dateKey)
        => DateTime.TryParseExact(dateKey, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : throw new BusinessException("Ngày tập không đúng định dạng (yyyy-MM-dd).");

    /// <summary>Tính lại tổng thời gian tập / thời gian nghỉ của từng bài và cả buổi.</summary>
    private static void RecalculateTotals(WorkoutSession session)
    {
        foreach (var exercise in session.Exercises)
        {
            exercise.TotalWorkSeconds = exercise.Sets.Sum(s => s.DurationSeconds);
            exercise.TotalExerciseRestSeconds = exercise.Sets.Sum(s => s.RestSeconds);
        }

        session.TotalDurationSeconds = session.Exercises.Sum(e => e.TotalWorkSeconds);
        session.TotalRestSeconds = session.Exercises.Sum(e => e.TotalExerciseRestSeconds);
    }

    private static WorkoutSessionDto MapToDto(WorkoutSession session)
    {
        var exercises = session.Exercises
            .OrderBy(e => e.OrderIndex)
            .Select(exercise => new SessionExerciseDto
            {
                WorkoutSessionExerciseId = exercise.WorkoutSessionExerciseId,
                OrderIndex = exercise.OrderIndex,
                ExerciseId = exercise.ExerciseId,
                ExerciseName = exercise.ExerciseName,
                MuscleGroup = exercise.MuscleGroup,
                TargetSets = exercise.TargetSets,
                TargetReps = exercise.TargetReps,
                RestSeconds = exercise.RestSeconds,
                Note = exercise.Note,
                TotalWorkSeconds = exercise.TotalWorkSeconds,
                TotalExerciseRestSeconds = exercise.TotalExerciseRestSeconds,
                IsCompleted = exercise.IsCompleted,
                Sets = exercise.Sets
                    .OrderBy(s => s.SetNumber)
                    .Select(set => new WorkoutSetLogDto
                    {
                        WorkoutSetLogId = set.WorkoutSetLogId,
                        SetNumber = set.SetNumber,
                        Reps = set.Reps,
                        DurationSeconds = set.DurationSeconds,
                        RestSeconds = set.RestSeconds,
                        StartedAt = set.StartedAt,
                        FinishedAt = set.FinishedAt
                    })
                    .ToList()
            })
            .ToList();

        var completed = exercises.Count(e => e.IsCompleted);
        var total = exercises.Count;

        return new WorkoutSessionDto
        {
            WorkoutSessionId = session.WorkoutSessionId,
            SessionDate = session.SessionDate,
            DateKey = session.SessionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            WorkoutPlanId = session.WorkoutPlanId,
            PlanName = session.PlanName,
            StartedAt = session.StartedAt,
            FinishedAt = session.FinishedAt,
            Status = session.Status,
            Note = session.Note,
            TotalDurationSeconds = session.TotalDurationSeconds,
            TotalRestSeconds = session.TotalRestSeconds,
            CompletedExercises = completed,
            TotalExercises = total,
            ProgressPercent = total == 0 ? 0 : (int)Math.Round(completed * 100.0 / total),
            Exercises = exercises
        };
    }
}
