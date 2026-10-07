using System;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.ClassDTOs;

namespace GYM_Management_System.Services.ClassServices;

public interface IClassBookingService
{
    Task<PaginatedResponse<ClassBookingDto>> GetBookingsAsync(ClassBookingQueryParameters query);

    Task<ClassBookingDto> GetBookingByIdAsync(Guid bookingId);

    Task<ClassBookingDto> CreateBookingAsync(CreateBookingRequest request, Guid? currentUserId);

    Task<ClassBookingDto> UpdateStatusAsync(Guid bookingId, UpdateBookingStatusRequest request);

    Task<ClassBookingDto> CancelBookingAsync(Guid bookingId);
}
