using System;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.ClassDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.ClassServices;

public class ClassBookingService : IClassBookingService
{
    private readonly GymDbContext _context;

    public ClassBookingService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<ClassBookingDto>> GetBookingsAsync(ClassBookingQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var bookingsQuery = _context.ClassBookings
            .AsNoTracking()
            .Include(b => b.TrainingClass)
            .Include(b => b.Member)
            .AsQueryable();

        if (query.ClassId.HasValue) bookingsQuery = bookingsQuery.Where(b => b.ClassId == query.ClassId);
        if (query.MemberId.HasValue) bookingsQuery = bookingsQuery.Where(b => b.MemberId == query.MemberId);
        if (!string.IsNullOrWhiteSpace(query.Status)) bookingsQuery = bookingsQuery.Where(b => b.Status == query.Status);
        if (query.FromDate.HasValue) bookingsQuery = bookingsQuery.Where(b => b.BookedAt >= query.FromDate);
        if (query.ToDate.HasValue) bookingsQuery = bookingsQuery.Where(b => b.BookedAt <= query.ToDate);

        var totalCount = await bookingsQuery.CountAsync();

        var bookings = await bookingsQuery
            .OrderByDescending(b => b.BookedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = bookings.Select(MapToDto).ToList();

        return PaginatedResponse<ClassBookingDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<ClassBookingDto> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _context.ClassBookings
            .AsNoTracking()
            .Include(b => b.TrainingClass)
            .Include(b => b.Member)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId)
            ?? throw new KeyNotFoundException("Không tìm thấy đăng ký lớp học.");

        return MapToDto(booking);
    }

    public async Task<ClassBookingDto> CreateBookingAsync(CreateBookingRequest request, Guid? currentUserId)
    {
        var trainingClass = await _context.TrainingClasses
            .FirstOrDefaultAsync(c => c.ClassId == request.ClassId)
            ?? throw new KeyNotFoundException("Không tìm thấy lớp học.");

        if (!trainingClass.IsActive)
            throw new BusinessException("Lớp học đã ngừng hoạt động.");

        var member = await _context.Members.FirstOrDefaultAsync(m => m.MemberId == request.MemberId)
            ?? throw new KeyNotFoundException("Không tìm thấy hội viên.");

        if (member.Status != DomainConstants.MemberStatus.Active)
            throw new BusinessException("Hội viên đang không ở trạng thái hoạt động.");

        var hasActiveSubscription = await _context.MemberSubscriptions
            .AnyAsync(s => s.MemberId == member.MemberId && s.Status == DomainConstants.SubscriptionStatus.Active);

        if (!hasActiveSubscription)
            throw new BusinessException("Hội viên chưa có gói tập đang hiệu lực nên không thể đăng ký lớp.");

        if (request.ScheduleId.HasValue)
        {
            var scheduleExists = await _context.ClassSchedules
                .AnyAsync(s => s.ScheduleId == request.ScheduleId && s.ClassId == request.ClassId);

            if (!scheduleExists)
                throw new BusinessException("Lịch học không thuộc lớp này.");
        }

        var duplicated = await _context.ClassBookings
            .AnyAsync(b => b.ClassId == request.ClassId
                           && b.MemberId == request.MemberId
                           && b.Status == DomainConstants.BookingStatus.Booked
                           && (request.ScheduleId == null || b.ScheduleId == request.ScheduleId));

        if (duplicated)
            throw new BusinessException("Hội viên đã đăng ký lớp này.");

        var currentBooked = await _context.ClassBookings
            .CountAsync(b => b.ClassId == request.ClassId && b.Status == DomainConstants.BookingStatus.Booked);

        if (currentBooked >= trainingClass.Capacity)
            throw new BusinessException("Lớp học đã đầy chỗ.");

        var booking = new ClassBooking
        {
            BookingId = Guid.NewGuid(),
            ClassId = request.ClassId,
            ScheduleId = request.ScheduleId,
            MemberId = request.MemberId,
            BookedAt = DateTime.UtcNow,
            Status = DomainConstants.BookingStatus.Booked,
            Notes = request.Notes,
            CreatedBy = currentUserId
        };

        _context.ClassBookings.Add(booking);
        await _context.SaveChangesAsync();

        await _context.Entry(booking).Reference(b => b.TrainingClass).LoadAsync();
        await _context.Entry(booking).Reference(b => b.Member).LoadAsync();

        return MapToDto(booking);
    }

    public async Task<ClassBookingDto> UpdateStatusAsync(Guid bookingId, UpdateBookingStatusRequest request)
    {
        var validStatuses = new[]
        {
            DomainConstants.BookingStatus.Booked,
            DomainConstants.BookingStatus.Attended,
            DomainConstants.BookingStatus.Cancelled,
            DomainConstants.BookingStatus.NoShow
        };

        if (!validStatuses.Contains(request.Status))
            throw new BusinessException($"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}.");

        var booking = await _context.ClassBookings
            .Include(b => b.TrainingClass)
            .Include(b => b.Member)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId)
            ?? throw new KeyNotFoundException("Không tìm thấy đăng ký lớp học.");

        booking.Status = request.Status;
        if (request.Status == DomainConstants.BookingStatus.Cancelled)
            booking.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(booking);
    }

    public async Task<ClassBookingDto> CancelBookingAsync(Guid bookingId)
        => await UpdateStatusAsync(bookingId, new UpdateBookingStatusRequest { Status = DomainConstants.BookingStatus.Cancelled });

    private static ClassBookingDto MapToDto(ClassBooking booking) => new()
    {
        BookingId = booking.BookingId,
        ClassId = booking.ClassId,
        ClassName = booking.TrainingClass?.ClassName,
        ScheduleId = booking.ScheduleId,
        MemberId = booking.MemberId,
        MemberName = booking.Member?.FullName,
        MemberCode = booking.Member?.MemberCode,
        BookedAt = booking.BookedAt,
        Status = booking.Status,
        CancelledAt = booking.CancelledAt,
        Notes = booking.Notes
    };
}
