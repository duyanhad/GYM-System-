using System;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.TrainerDTOs;

namespace GYM_Management_System.Services.TrainerServices;

public interface ITrainerService
{
    Task<PaginatedResponse<TrainerDto>> GetTrainersAsync(TrainerQueryParameters query);

    Task<TrainerDto> GetTrainerByIdAsync(Guid trainerId);

    Task<TrainerDto> CreateTrainerAsync(CreateTrainerRequest request);

    Task<TrainerDto> UpdateTrainerAsync(Guid trainerId, UpdateTrainerRequest request);

    Task<TrainerDto> ChangeActiveAsync(Guid trainerId, bool isActive);

    Task DeleteTrainerAsync(Guid trainerId);
}
