using System;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.ClassDTOs;

namespace GYM_Management_System.Services.ClassServices;

public interface ITrainingClassService
{
    Task<PaginatedResponse<TrainingClassDto>> GetClassesAsync(TrainingClassQueryParameters query);

    Task<TrainingClassDto> GetClassByIdAsync(Guid classId);

    Task<TrainingClassDto> CreateClassAsync(CreateTrainingClassRequest request);

    Task<TrainingClassDto> UpdateClassAsync(Guid classId, UpdateTrainingClassRequest request);

    Task<TrainingClassDto> ChangeActiveAsync(Guid classId, bool isActive);

    Task DeleteClassAsync(Guid classId);
}
