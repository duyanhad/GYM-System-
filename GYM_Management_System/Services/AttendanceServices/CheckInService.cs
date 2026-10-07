using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.AttendanceDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.AttendanceServices;

public class CheckInService : ICheckInService
{
    private readonly GymDbContext _context;
    private readonly INotificationSender _notificationSender;

    public CheckInService(GymDbContext context, INotificationSender notificationSender)
    {
        _context = context;
        _notificationSender = notificationSender;
    }

    public async Task<PaginatedResponse<CheckInDto>> GetCheckInsAsync(AttendanceQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var checkInsQuery = _context.CheckIns
            .AsNoTracking()
            .Include(c => c.Member)
            .Include(c => c.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            checkInsQuery = checkInsQuery.Where(c =>
                c.Member.FullName.ToLower().Contains(keyword) ||
                c.Member.MemberCode.ToLower().Contains(keyword) ||
                c.Member.Phone.ToLower().Contains(keyword));
        }

        if (query.MemberId.HasValue) checkInsQuery = checkInsQuery.Where(c => c.MemberId == query.MemberId);
        if (query.BranchId.HasValue) checkInsQuery = checkInsQuery.Where(c => c.BranchId == query.BranchId);
        if (query.FromDate.HasValue) checkInsQuery = checkInsQuery.Where(c => c.CheckInTime >= query.FromDate);
        if (query.ToDate.HasValue) checkInsQuery = checkInsQuery.Where(c => c.CheckInTime <= query.ToDate);
        if (query.StillInside == true) checkInsQuery = checkInsQuery.Where(c => c.CheckOutTime == null);

        var totalCount = await checkInsQuery.CountAsync();

        var checkIns = await checkInsQuery
            .OrderByDescending(c => c.CheckInTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = checkIns.Select(MapToDto).ToList();

        return PaginatedResponse<CheckInDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<CheckInDto> CheckInAsync(CreateCheckInRequest request, Guid? currentUserId)
    {
        Member? member = null;

        if (request.MemberId.HasValue)
        {
            member = await _context.Members.FirstOrDefaultAsync(m => m.MemberId == request.MemberId);
        }
        else if (!string.IsNullOrWhiteSpace(request.MemberCode))
        {
            var code = request.MemberCode.Trim().ToLower();
            member = await _context.Members.FirstOrDefaultAsync(m =>
                m.MemberCode.ToLower() == code || m.Phone == request.MemberCode.Trim());
        }

        if (member == null)
            throw new KeyNotFoundException("Không tìm thấy hội viên với mã/số điện thoại đã nhập.");

        if (member.Status != DomainConstants.MemberStatus.Active)
            throw new BusinessException("Hội viên đang không ở trạng thái hoạt động.");

        var subscription = await _context.MemberSubscriptions
            .AsNoTracking()
            .Where(s => s.MemberId == member.MemberId && s.Status == DomainConstants.SubscriptionStatus.Active)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (subscription == null)
            throw new BusinessException("Hội viên chưa có gói tập đang hiệu lực. Vui lòng gia hạn gói tập trước khi vào tập.");

        if (subscription.EndDate <= DateTime.UtcNow)
            throw new BusinessException("Gói tập của hội viên đã hết hạn. Vui lòng gia hạn để tiếp tục tập luyện.");

        var openCheckIn = await _context.CheckIns
            .FirstOrDefaultAsync(c => c.MemberId == member.MemberId && c.CheckOutTime == null);

        if (openCheckIn != null)
            throw new BusinessException("Hội viên đang có lượt check-in chưa check-out. Vui lòng check-out trước.");

        var now = DateTime.UtcNow;

        var checkIn = new CheckIn
        {
            CheckInId = Guid.NewGuid(),
            MemberId = member.MemberId,
            BranchId = request.BranchId ?? member.BranchId,
            CheckInTime = now,
            Method = request.Method,
            Notes = request.Notes,
            CreatedBy = currentUserId,
            CreatedAt = now
        };

        _context.CheckIns.Add(checkIn);
        await _context.SaveChangesAsync();

        await _context.Entry(checkIn).Reference(c => c.Member).LoadAsync();
        if (checkIn.BranchId.HasValue)
            await _context.Entry(checkIn).Reference(c => c.Branch).LoadAsync();

        var dto = MapToDto(checkIn);

        if (checkIn.BranchId.HasValue)
            await _notificationSender.SendToBranchAsync(
                checkIn.BranchId.Value.ToString(),
                "memberCheckedIn",
                new { dto.MemberName, dto.MemberCode, dto.CheckInTime });

        return dto;
    }

    public async Task<CheckInDto> CheckOutAsync(CheckOutRequest request)
    {
        var checkIn = await _context.CheckIns
            .Include(c => c.Member)
            .Include(c => c.Branch)
            .FirstOrDefaultAsync(c => c.CheckInId == request.CheckInId)
            ?? throw new KeyNotFoundException("Không tìm thấy lượt check-in.");

        if (checkIn.CheckOutTime.HasValue)
            throw new BusinessException("Lượt check-in này đã được check-out trước đó.");

        checkIn.CheckOutTime = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            checkIn.Notes = request.Notes;

        await _context.SaveChangesAsync();

        return MapToDto(checkIn);
    }

    public async Task<AttendanceStatsDto> GetStatsAsync(DateTime? date, Guid? branchId)
    {
        var dayStart = date.HasValue
            ? VietnamTime.ToUtc(date.Value.Date)
            : VietnamTime.TodayStartUtc;
        var dayEnd = dayStart.AddDays(1);

        var query = _context.CheckIns
            .AsNoTracking()
            .Where(c => c.CheckInTime >= dayStart && c.CheckInTime < dayEnd);

        if (branchId.HasValue)
            query = query.Where(c => c.BranchId == branchId);

        var checkIns = await query
            .Select(c => new { c.MemberId, c.CheckInTime, c.CheckOutTime })
            .ToListAsync();

        var durations = checkIns
            .Where(c => c.CheckOutTime.HasValue)
            .Select(c => (c.CheckOutTime!.Value - c.CheckInTime).TotalMinutes)
            .ToList();

        return new AttendanceStatsDto
        {
            Date = dayStart,
            TotalCheckIns = checkIns.Count,
            CurrentlyInside = checkIns.Count(c => c.CheckOutTime == null),
            UniqueMembers = checkIns.Select(c => c.MemberId).Distinct().Count(),
            AverageDurationMinutes = durations.Count > 0 ? Math.Round(durations.Average(), 1) : 0,
            Hourly = Enumerable.Range(0, 24)
                .Select(hour => new HourlyAttendanceDto
                {
                    Hour = hour,
                    CheckIns = checkIns.Count(c => c.CheckInTime >= dayStart.AddHours(hour)
                                                   && c.CheckInTime < dayStart.AddHours(hour + 1))
                })
                .Where(x => x.CheckIns > 0)
                .ToList()
        };
    }

    private static CheckInDto MapToDto(CheckIn checkIn) => new()
    {
        CheckInId = checkIn.CheckInId,
        MemberId = checkIn.MemberId,
        MemberName = checkIn.Member?.FullName,
        MemberCode = checkIn.Member?.MemberCode,
        MemberPhone = checkIn.Member?.Phone,
        BranchId = checkIn.BranchId,
        BranchName = checkIn.Branch?.BranchName,
        CheckInTime = checkIn.CheckInTime,
        CheckOutTime = checkIn.CheckOutTime,
        DurationMinutes = checkIn.CheckOutTime.HasValue
            ? (int)(checkIn.CheckOutTime.Value - checkIn.CheckInTime).TotalMinutes
            : null,
        Method = checkIn.Method,
        Notes = checkIn.Notes
    };
}
