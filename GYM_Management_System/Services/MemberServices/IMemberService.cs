using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.MemberDTOs;

namespace GYM_Management_System.Services.MemberServices;

public interface IMemberService
{
    Task<PaginatedResponse<MemberDto>> GetMembersAsync(MemberQueryParameters query);

    Task<MemberDetailDto> GetMemberByIdAsync(Guid memberId);

    Task<MemberDto> CreateMemberAsync(CreateMemberRequest request);

    Task<MemberDto> UpdateMemberAsync(Guid memberId, UpdateMemberRequest request);

    Task<MemberDto> ChangeStatusAsync(Guid memberId, ChangeMemberStatusRequest request);

    Task DeleteMemberAsync(Guid memberId);

    Task<List<MemberSubscriptionHistoryDto>> GetSubscriptionHistoryAsync(Guid memberId);
}
