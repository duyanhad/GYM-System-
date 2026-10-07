using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.WorkoutDTOs;

namespace GYM_Management_System.Services.WorkoutServices;

public interface IWorkoutExerciseService
{
    Task<List<ExerciseDto>> GetExercisesAsync(Guid userId);

    Task<ExerciseDto> CreateExerciseAsync(Guid userId, CreateExerciseRequest request);

    Task<ExerciseDto> UpdateExerciseAsync(Guid userId, Guid exerciseId, UpdateExerciseRequest request);

    Task DeleteExerciseAsync(Guid userId, Guid exerciseId);
}
