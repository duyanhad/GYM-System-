using System;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.ClassDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.ClassServices;

public class TrainingClassService : ITrainingClassService
{
    private readonly GymDbContext _context;

    public TrainingClassService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<TrainingClassDto>> GetClassesAsync(TrainingClassQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var classesQuery = _context.TrainingClasses
            .AsNoTracking()
            .Include(c => c.Trainer)
            .Include(c => c.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            classesQuery = classesQuery.Where(c =>
                c.ClassName.ToLower().Contains(keyword) ||
                c.ClassCode.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(query.Level))
            classesQuery = classesQuery.Where(c => c.Level == query.Level);

        if (query.TrainerId.HasValue)
            classesQuery = classesQuery.Where(c => c.TrainerId == query.TrainerId);

        if (query.BranchId.HasValue)
            classesQuery = classesQuery.Where(c => c.BranchId == query.BranchId);

        if (query.IsActive.HasValue)
            classesQuery = classesQuery.Where(c => c.IsActive == query.IsActive);

        var totalCount = await classesQuery.CountAsync();

        var classes = await classesQuery
            .OrderBy(c => c.ClassName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var classIds = classes.Select(c => c.ClassId).ToList();
        var scheduleCounts = await _context.ClassSchedules
            .AsNoTracking()
            .Where(s => classIds.Contains(s.ClassId))
            .GroupBy(s => s.ClassId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassId, x => x.Count);

        var items = classes.Select(c => MapToDto(c, scheduleCounts.GetValueOrDefault(c.ClassId))).ToList();

        return PaginatedResponse<TrainingClassDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<TrainingClassDto> GetClassByIdAsync(Guid classId)
    {
        var trainingClass = await _context.TrainingClasses
            .AsNoTracking()
            .Include(c => c.Trainer)
            .Include(c => c.Branch)
            .FirstOrDefaultAsync(c => c.ClassId == classId)
            ?? throw new KeyNotFoundException("Không tìm thấy lớp học.");

        var scheduleCount = await _context.ClassSchedules.CountAsync(s => s.ClassId == classId);

        return MapToDto(trainingClass, scheduleCount);
    }

    public async Task<TrainingClassDto> CreateClassAsync(CreateTrainingClassRequest request)
    {
        var classCode = request.ClassCode.Trim();

        if (await _context.TrainingClasses.AnyAsync(c => c.ClassCode.ToLower() == classCode.ToLower()))
            throw new BusinessException("Mã lớp học đã tồn tại.");

        var validLevels = new[] { DomainConstants.ClassLevel.Beginner, DomainConstants.ClassLevel.Intermediate, DomainConstants.ClassLevel.Advanced };
        if (!validLevels.Contains(request.Level))
            throw new BusinessException($"Trình độ lớp không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validLevels)}.");

        if (request.TrainerId.HasValue && !await _context.Trainers.AnyAsync(t => t.TrainerId == request.TrainerId))
            throw new BusinessException("Huấn luyện viên không tồn tại.");

        if (request.BranchId.HasValue && !await _context.Branches.AnyAsync(b => b.BranchId == request.BranchId))
            throw new BusinessException("Chi nhánh không tồn tại.");

        var now = DateTime.UtcNow;

        var trainingClass = new TrainingClass
        {
            ClassId = Guid.NewGuid(),
            ClassCode = classCode,
            ClassName = request.ClassName.Trim(),
            Description = request.Description,
            Level = request.Level,
            TrainerId = request.TrainerId,
            BranchId = request.BranchId,
            Capacity = request.Capacity,
            DurationMinutes = request.DurationMinutes,
            PricePerSession = request.PricePerSession,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.TrainingClasses.Add(trainingClass);
        await _context.SaveChangesAsync();

        if (trainingClass.TrainerId.HasValue)
            await _context.Entry(trainingClass).Reference(c => c.Trainer).LoadAsync();
        if (trainingClass.BranchId.HasValue)
            await _context.Entry(trainingClass).Reference(c => c.Branch).LoadAsync();

        return MapToDto(trainingClass, 0);
    }

    public async Task<TrainingClassDto> UpdateClassAsync(Guid classId, UpdateTrainingClassRequest request)
    {
        var trainingClass = await _context.TrainingClasses
            .Include(c => c.Trainer)
            .Include(c => c.Branch)
            .FirstOrDefaultAsync(c => c.ClassId == classId)
            ?? throw new KeyNotFoundException("Không tìm thấy lớp học.");

        if (!string.IsNullOrWhiteSpace(request.ClassName)) trainingClass.ClassName = request.ClassName.Trim();
        if (request.Description != null) trainingClass.Description = request.Description;

        if (!string.IsNullOrWhiteSpace(request.Level))
        {
            var validLevels = new[] { DomainConstants.ClassLevel.Beginner, DomainConstants.ClassLevel.Intermediate, DomainConstants.ClassLevel.Advanced };
            if (!validLevels.Contains(request.Level))
                throw new BusinessException($"Trình độ lớp không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validLevels)}.");

            trainingClass.Level = request.Level;
        }

        if (request.TrainerId.HasValue)
        {
            if (!await _context.Trainers.AnyAsync(t => t.TrainerId == request.TrainerId))
                throw new BusinessException("Huấn luyện viên không tồn tại.");

            trainingClass.TrainerId = request.TrainerId;
        }

        if (request.BranchId.HasValue) trainingClass.BranchId = request.BranchId;
        if (request.Capacity.HasValue && request.Capacity.Value > 0) trainingClass.Capacity = request.Capacity.Value;
        if (request.DurationMinutes.HasValue && request.DurationMinutes.Value > 0) trainingClass.DurationMinutes = request.DurationMinutes.Value;
        if (request.PricePerSession.HasValue && request.PricePerSession.Value >= 0) trainingClass.PricePerSession = request.PricePerSession.Value;
        if (request.IsActive.HasValue) trainingClass.IsActive = request.IsActive.Value;

        trainingClass.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _context.Entry(trainingClass).Reference(c => c.Trainer).LoadAsync();
        if (trainingClass.BranchId.HasValue)
            await _context.Entry(trainingClass).Reference(c => c.Branch).LoadAsync();

        var scheduleCount = await _context.ClassSchedules.CountAsync(s => s.ClassId == classId);

        return MapToDto(trainingClass, scheduleCount);
    }

    public async Task<TrainingClassDto> ChangeActiveAsync(Guid classId, bool isActive)
    {
        var trainingClass = await _context.TrainingClasses
            .Include(c => c.Trainer)
            .Include(c => c.Branch)
            .FirstOrDefaultAsync(c => c.ClassId == classId)
            ?? throw new KeyNotFoundException("Không tìm thấy lớp học.");

        trainingClass.IsActive = isActive;
        trainingClass.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var scheduleCount = await _context.ClassSchedules.CountAsync(s => s.ClassId == classId);

        return MapToDto(trainingClass, scheduleCount);
    }

    public async Task DeleteClassAsync(Guid classId)
    {
        var trainingClass = await _context.TrainingClasses.FirstOrDefaultAsync(c => c.ClassId == classId)
            ?? throw new KeyNotFoundException("Không tìm thấy lớp học.");

        var hasBookings = await _context.ClassBookings.AnyAsync(b => b.ClassId == classId);
        if (hasBookings)
            throw new BusinessException("Lớp học đã có hội viên đăng ký nên không thể xóa. Hãy chuyển lớp sang trạng thái ngừng hoạt động.");

        _context.TrainingClasses.Remove(trainingClass);
        await _context.SaveChangesAsync();
    }

    private static TrainingClassDto MapToDto(TrainingClass trainingClass, int schedulesCount) => new()
    {
        ClassId = trainingClass.ClassId,
        ClassCode = trainingClass.ClassCode,
        ClassName = trainingClass.ClassName,
        Description = trainingClass.Description,
        Level = trainingClass.Level,
        TrainerId = trainingClass.TrainerId,
        TrainerName = trainingClass.Trainer?.FullName,
        BranchId = trainingClass.BranchId,
        BranchName = trainingClass.Branch?.BranchName,
        Capacity = trainingClass.Capacity,
        DurationMinutes = trainingClass.DurationMinutes,
        PricePerSession = trainingClass.PricePerSession,
        IsActive = trainingClass.IsActive,
        SchedulesCount = schedulesCount,
        CreatedAt = trainingClass.CreatedAt
    };
}
