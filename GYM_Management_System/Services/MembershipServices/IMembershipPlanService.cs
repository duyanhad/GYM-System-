using System;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.MembershipDTOs;

namespace GYM_Management_System.Services.MembershipServices;

public interface IMembershipPlanService
{
    Task<PaginatedResponse<MembershipPlanDto>> GetPlansAsync(MembershipPlanQueryParameters query);

    Task<MembershipPlanDto> GetPlanByIdAsync(Guid planId);

    Task<MembershipPlanDto> CreatePlanAsync(CreateMembershipPlanRequest request);

    Task<MembershipPlanDto> UpdatePlanAsync(Guid planId, UpdateMembershipPlanRequest request);

    Task<MembershipPlanDto> ChangeActiveAsync(Guid planId, bool isActive);

    Task DeletePlanAsync(Guid planId);
}
