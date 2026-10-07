using System;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.AttendanceDTOs;

namespace GYM_Management_System.Services.AttendanceServices;

public interface ICheckInService
{
    Task<PaginatedResponse<CheckInDto>> GetCheckInsAsync(AttendanceQueryParameters query);

    Task<CheckInDto> CheckInAsync(CreateCheckInRequest request, Guid? currentUserId);

    Task<CheckInDto> CheckOutAsync(CheckOutRequest request);

    Task<AttendanceStatsDto> GetStatsAsync(DateTime? date, Guid? branchId);
}
