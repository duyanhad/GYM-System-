using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.ClassDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.ClassServices;

public class ClassScheduleService : IClassScheduleService
{
    private static readonly string[] VietnameseDayNames =
    {
        "Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy"
    };

    private readonly GymDbContext _context;

    public ClassScheduleService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<List<ClassScheduleDto>> GetSchedulesAsync(Guid? classId, Guid? trainerId, int? dayOfWeek, bool? isActive)
    {
        var query = _context.ClassSchedules
            .AsNoTracking()
            .Include(s => s.TrainingClass)
            .Include(s => s.Trainer)
            .AsQueryable();

        if (classId.HasValue) query = query.Where(s => s.ClassId == classId);
        if (trainerId.HasValue) query = query.Where(s => s.TrainerId == trainerId);
        if (dayOfWeek.HasValue) query = query.Where(s => (int)s.DayOfWeek == dayOfWeek);
        if (isActive.HasValue) query = query.Where(s => s.IsActive == isActive);

        var schedules = await query.ToListAsync();

        var scheduleIds = schedules.Select(s => s.ScheduleId).ToList();
        var bookingCounts = await _context.ClassBookings
            .AsNoTracking()
            .Where(b => b.ScheduleId != null && scheduleIds.Contains(b.ScheduleId.Value)
                        && b.Status == DomainConstants.BookingStatus.Booked)
            .GroupBy(b => b.ScheduleId!.Value)
            .Select(g => new { ScheduleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ScheduleId, x => x.Count);

        return schedules
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(s => MapToDto(s, bookingCounts.GetValueOrDefault(s.ScheduleId)))
            .ToList();
    }

    public async Task<ClassScheduleDto> GetScheduleByIdAsync(Guid scheduleId)
    {
        var schedule = await _context.ClassSchedules
            .AsNoTracking()
            .Include(s => s.TrainingClass)
            .Include(s => s.Trainer)
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId)
            ?? throw new KeyNotFoundException("Không tìm thấy lịch học.");

        var bookedCount = await _context.ClassBookings
            .CountAsync(b => b.ScheduleId == scheduleId && b.Status == DomainConstants.BookingStatus.Booked);

        return MapToDto(schedule, bookedCount);
    }

    public async Task<List<WeeklyScheduleDto>> GetWeeklyTimetableAsync(Guid? branchId)
    {
        var query = _context.ClassSchedules
            .AsNoTracking()
            .Include(s => s.TrainingClass)
            .Include(s => s.Trainer)
            .Where(s => s.IsActive && s.TrainingClass.IsActive);

        if (branchId.HasValue)
            query = query.Where(s => s.TrainingClass.BranchId == branchId);

        var schedules = await query.ToListAsync();

        var result = new List<WeeklyScheduleDto>();

        foreach (var day in Enumerable.Range(0, 7))
        {
            result.Add(new WeeklyScheduleDto
            {
                DayOfWeek = day,
                DayOfWeekName = VietnameseDayNames[day],
                Schedules = schedules
                    .Where(s => (int)s.DayOfWeek == day)
                    .OrderBy(s => s.StartTime)
                    .Select(s => MapToDto(s, 0))
                    .ToList()
            });
        }

        return result;
    }

    public async Task<ClassScheduleDto> CreateScheduleAsync(CreateClassScheduleRequest request)
    {
        if (!await _context.TrainingClasses.AnyAsync(c => c.ClassId == request.ClassId))
            throw new KeyNotFoundException("Không tìm thấy lớp học.");

        if (request.EndTime <= request.StartTime)
            throw new BusinessException("Giờ kết thúc phải sau giờ bắt đầu.");

        if (request.TrainerId.HasValue && !await _context.Trainers.AnyAsync(t => t.TrainerId == request.TrainerId))
            throw new BusinessException("Huấn luyện viên không tồn tại.");

        await EnsureNoConflictAsync(request.ClassId, request.TrainerId, request.DayOfWeek, request.StartTime, request.EndTime, null);

        var schedule = new ClassSchedule
        {
            ScheduleId = Guid.NewGuid(),
            ClassId = request.ClassId,
            TrainerId = request.TrainerId,
            Room = request.Room?.Trim(),
            DayOfWeek = (DayOfWeek)request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ClassSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        await _context.Entry(schedule).Reference(s => s.TrainingClass).LoadAsync();
        if (schedule.TrainerId.HasValue)
            await _context.Entry(schedule).Reference(s => s.Trainer).LoadAsync();

        return MapToDto(schedule, 0);
    }

    public async Task<ClassScheduleDto> UpdateScheduleAsync(Guid scheduleId, UpdateClassScheduleRequest request)
    {
        var schedule = await _context.ClassSchedules
            .Include(s => s.TrainingClass)
            .Include(s => s.Trainer)
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId)
            ?? throw new KeyNotFoundException("Không tìm thấy lịch học.");

        var dayOfWeek = request.DayOfWeek.HasValue ? (DayOfWeek)request.DayOfWeek.Value : schedule.DayOfWeek;
        var startTime = request.StartTime ?? schedule.StartTime;
        var endTime = request.EndTime ?? schedule.EndTime;
        var trainerId = request.TrainerId ?? schedule.TrainerId;

        if (endTime <= startTime)
            throw new BusinessException("Giờ kết thúc phải sau giờ bắt đầu.");

        if (request.TrainerId.HasValue && !await _context.Trainers.AnyAsync(t => t.TrainerId == request.TrainerId))
            throw new BusinessException("Huấn luyện viên không tồn tại.");

        await EnsureNoConflictAsync(schedule.ClassId, trainerId, (int)dayOfWeek, startTime, endTime, scheduleId);

        schedule.DayOfWeek = dayOfWeek;
        schedule.StartTime = startTime;
        schedule.EndTime = endTime;
        schedule.TrainerId = trainerId;
        if (request.Room != null) schedule.Room = request.Room.Trim();
        if (request.EffectiveFrom.HasValue) schedule.EffectiveFrom = request.EffectiveFrom;
        if (request.EffectiveTo.HasValue) schedule.EffectiveTo = request.EffectiveTo;
        if (request.IsActive.HasValue) schedule.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        await _context.Entry(schedule).Reference(s => s.Trainer).LoadAsync();

        var bookedCount = await _context.ClassBookings
            .CountAsync(b => b.ScheduleId == scheduleId && b.Status == DomainConstants.BookingStatus.Booked);

        return MapToDto(schedule, bookedCount);
    }

    public async Task DeleteScheduleAsync(Guid scheduleId)
    {
        var schedule = await _context.ClassSchedules.FirstOrDefaultAsync(s => s.ScheduleId == scheduleId)
            ?? throw new KeyNotFoundException("Không tìm thấy lịch học.");

        var hasBookings = await _context.ClassBookings
            .AnyAsync(b => b.ScheduleId == scheduleId && b.Status == DomainConstants.BookingStatus.Booked);

        if (hasBookings)
            throw new BusinessException("Lịch học đã có hội viên đăng ký nên không thể xóa. Hãy tạm ngưng lịch học.");

        _context.ClassSchedules.Remove(schedule);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Chặn trùng lịch: cùng phòng (không kiểm tra vì phòng để dạng text) hoặc cùng huấn luyện viên
    /// trong cùng khung giờ của cùng một ngày.
    /// </summary>
    private async Task EnsureNoConflictAsync(
        Guid classId,
        Guid? trainerId,
        int dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime,
        Guid? ignoreScheduleId)
    {
        if (trainerId.HasValue)
        {
            var trainerConflict = await _context.ClassSchedules
                .Include(s => s.TrainingClass)
                .Where(s => s.TrainerId == trainerId
                            && (int)s.DayOfWeek == dayOfWeek
                            && s.IsActive
                            && (!ignoreScheduleId.HasValue || s.ScheduleId != ignoreScheduleId))
                .AnyAsync(s => startTime < s.EndTime && endTime > s.StartTime);

            if (trainerConflict)
                throw new BusinessException("Huấn luyện viên đã có lịch dạy trùng khung giờ này.");
        }

        var classConflict = await _context.ClassSchedules
            .Where(s => s.ClassId == classId
                        && (int)s.DayOfWeek == dayOfWeek
                        && s.IsActive
                        && (!ignoreScheduleId.HasValue || s.ScheduleId != ignoreScheduleId))
            .AnyAsync(s => startTime < s.EndTime && endTime > s.StartTime);

        if (classConflict)
            throw new BusinessException("Lớp học đã có lịch trùng khung giờ này.");
    }

    private static ClassScheduleDto MapToDto(ClassSchedule schedule, int bookedCount) => new()
    {
        ScheduleId = schedule.ScheduleId,
        ClassId = schedule.ClassId,
        ClassName = schedule.TrainingClass?.ClassName,
        TrainerId = schedule.TrainerId,
        TrainerName = schedule.Trainer?.FullName,
        Room = schedule.Room,
        DayOfWeek = (int)schedule.DayOfWeek,
        DayOfWeekName = VietnameseDayNames[(int)schedule.DayOfWeek],
        StartTime = schedule.StartTime,
        EndTime = schedule.EndTime,
        EffectiveFrom = schedule.EffectiveFrom,
        EffectiveTo = schedule.EffectiveTo,
        IsActive = schedule.IsActive,
        BookedCount = bookedCount
    };
}
