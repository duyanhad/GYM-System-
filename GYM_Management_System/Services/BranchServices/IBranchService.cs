using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.BranchDTOs;

namespace GYM_Management_System.Services.BranchServices;

public interface IBranchService
{
    Task<List<BranchDto>> GetBranchesAsync(bool? isActive);

    Task<BranchDto> GetBranchByIdAsync(Guid branchId);

    Task<BranchDto> CreateBranchAsync(CreateBranchRequest request);

    Task<BranchDto> UpdateBranchAsync(Guid branchId, UpdateBranchRequest request);

    Task DeleteBranchAsync(Guid branchId);
}
