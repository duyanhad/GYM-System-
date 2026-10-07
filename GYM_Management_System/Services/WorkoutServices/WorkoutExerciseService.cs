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
/// Thư viện bài tập: bài tập mẫu của hệ thống (UserId = null) + bài tập riêng của người dùng.
/// </summary>
public class WorkoutExerciseService : IWorkoutExerciseService
{
    private readonly GymDbContext _context;

    public WorkoutExerciseService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExerciseDto>> GetExercisesAsync(Guid userId)
    {
        var exercises = await _context.Exercises
            .AsNoTracking()
            .Where(e => e.IsActive && (e.UserId == null || e.UserId == userId))
            .OrderBy(e => e.MuscleGroup)
            .ThenBy(e => e.Name)
            .ToListAsync();

        return exercises.Select(MapToDto).ToList();
    }

    public async Task<ExerciseDto> CreateExerciseAsync(Guid userId, CreateExerciseRequest request)
    {
        var name = request.Name.Trim();

        var duplicated = await _context.Exercises.AnyAsync(e =>
            e.IsActive && e.UserId == userId && e.Name.ToLower() == name.ToLower());

        if (duplicated)
            throw new BusinessException("Bạn đã có bài tập với tên này.");

        var now = DateTime.UtcNow;

        var exercise = new Exercise
        {
            ExerciseId = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            MuscleGroup = request.MuscleGroup.Trim(),
            DefaultSets = request.DefaultSets,
            DefaultReps = request.DefaultReps,
            DefaultRestSeconds = request.DefaultRestSeconds,
            Note = request.Note?.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        return MapToDto(exercise);
    }

    public async Task<ExerciseDto> UpdateExerciseAsync(Guid userId, Guid exerciseId, UpdateExerciseRequest request)
    {
        var exercise = await _context.Exercises.FirstOrDefaultAsync(e => e.ExerciseId == exerciseId)
            ?? throw new KeyNotFoundException("Không tìm thấy bài tập.");

        if (exercise.UserId != userId)
            throw new BusinessException("Chỉ có thể sửa bài tập do bạn tạo.");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();

            var duplicated = await _context.Exercises.AnyAsync(e =>
                e.ExerciseId != exerciseId && e.IsActive && e.UserId == userId && e.Name.ToLower() == name.ToLower());

            if (duplicated) throw new BusinessException("Bạn đã có bài tập với tên này.");

            exercise.Name = name;
        }

        if (!string.IsNullOrWhiteSpace(request.MuscleGroup)) exercise.MuscleGroup = request.MuscleGroup.Trim();
        if (request.DefaultSets.HasValue && request.DefaultSets.Value > 0) exercise.DefaultSets = request.DefaultSets.Value;
        if (request.DefaultReps.HasValue && request.DefaultReps.Value > 0) exercise.DefaultReps = request.DefaultReps.Value;
        if (request.DefaultRestSeconds.HasValue && request.DefaultRestSeconds.Value >= 0) exercise.DefaultRestSeconds = request.DefaultRestSeconds.Value;
        if (request.Note != null) exercise.Note = request.Note.Trim();

        exercise.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(exercise);
    }

    public async Task DeleteExerciseAsync(Guid userId, Guid exerciseId)
    {
        var exercise = await _context.Exercises.FirstOrDefaultAsync(e => e.ExerciseId == exerciseId)
            ?? throw new KeyNotFoundException("Không tìm thấy bài tập.");

        if (exercise.UserId != userId)
            throw new BusinessException("Chỉ có thể xoá bài tập do bạn tạo.");

        // Xoá mềm để không làm hỏng giáo án/buổi tập đã ghi
        exercise.IsActive = false;
        exercise.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private static ExerciseDto MapToDto(Exercise exercise) => new()
    {
        ExerciseId = exercise.ExerciseId,
        Name = exercise.Name,
        MuscleGroup = exercise.MuscleGroup,
        DefaultSets = exercise.DefaultSets,
        DefaultReps = exercise.DefaultReps,
        DefaultRestSeconds = exercise.DefaultRestSeconds,
        Note = exercise.Note,
        IsSystem = exercise.UserId == null
    };
}
