using System;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.TrainerDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.TrainerServices;

public class TrainerService : ITrainerService
{
    private readonly GymDbContext _context;

    public TrainerService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<TrainerDto>> GetTrainersAsync(TrainerQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var trainersQuery = _context.Trainers
            .AsNoTracking()
            .Include(t => t.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            trainersQuery = trainersQuery.Where(t =>
                t.FullName.ToLower().Contains(keyword) ||
                t.TrainerCode.ToLower().Contains(keyword) ||
                t.Phone.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(query.Specialization))
            trainersQuery = trainersQuery.Where(t => t.Specialization != null && t.Specialization.Contains(query.Specialization));

        if (query.BranchId.HasValue)
            trainersQuery = trainersQuery.Where(t => t.BranchId == query.BranchId);

        if (query.IsActive.HasValue)
            trainersQuery = trainersQuery.Where(t => t.IsActive == query.IsActive);

        var totalCount = await trainersQuery.CountAsync();

        var trainers = await trainersQuery
            .OrderBy(t => t.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var trainerIds = trainers.Select(t => t.TrainerId).ToList();
        var classCounts = await _context.TrainingClasses
            .AsNoTracking()
            .Where(c => c.TrainerId != null && trainerIds.Contains(c.TrainerId.Value))
            .GroupBy(c => c.TrainerId!.Value)
            .Select(g => new { TrainerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TrainerId, x => x.Count);

        var items = trainers.Select(t => MapToDto(t, classCounts.GetValueOrDefault(t.TrainerId))).ToList();

        return PaginatedResponse<TrainerDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<TrainerDto> GetTrainerByIdAsync(Guid trainerId)
    {
        var trainer = await _context.Trainers
            .AsNoTracking()
            .Include(t => t.Branch)
            .FirstOrDefaultAsync(t => t.TrainerId == trainerId)
            ?? throw new KeyNotFoundException("Không tìm thấy huấn luyện viên.");

        var classCount = await _context.TrainingClasses.CountAsync(c => c.TrainerId == trainerId);

        return MapToDto(trainer, classCount);
    }

    public async Task<TrainerDto> CreateTrainerAsync(CreateTrainerRequest request)
    {
        var phone = request.Phone.Trim();

        if (await _context.Trainers.AnyAsync(t => t.Phone == phone))
            throw new BusinessException("Số điện thoại này đã được dùng cho huấn luyện viên khác.");

        if (request.BranchId.HasValue && !await _context.Branches.AnyAsync(b => b.BranchId == request.BranchId))
            throw new BusinessException("Chi nhánh không tồn tại.");

        var now = DateTime.UtcNow;
        var sequence = await _context.Trainers.CountAsync() + 1;

        var trainer = new Trainer
        {
            TrainerId = Guid.NewGuid(),
            TrainerCode = CodeGenerator.TrainerCode(sequence),
            FullName = request.FullName.Trim(),
            Phone = phone,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            Specialization = request.Specialization?.Trim(),
            Bio = request.Bio?.Trim(),
            HourlyRate = request.HourlyRate,
            JoinDate = request.JoinDate ?? now,
            AvatarUrl = request.AvatarUrl?.Trim(),
            BranchId = request.BranchId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Trainers.Add(trainer);
        await _context.SaveChangesAsync();

        if (trainer.BranchId.HasValue)
            await _context.Entry(trainer).Reference(t => t.Branch).LoadAsync();

        return MapToDto(trainer, 0);
    }

    public async Task<TrainerDto> UpdateTrainerAsync(Guid trainerId, UpdateTrainerRequest request)
    {
        var trainer = await _context.Trainers
            .Include(t => t.Branch)
            .FirstOrDefaultAsync(t => t.TrainerId == trainerId)
            ?? throw new KeyNotFoundException("Không tìm thấy huấn luyện viên.");

        if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone.Trim() != trainer.Phone)
        {
            var phone = request.Phone.Trim();
            if (await _context.Trainers.AnyAsync(t => t.TrainerId != trainerId && t.Phone == phone))
                throw new BusinessException("Số điện thoại này đã được dùng cho huấn luyện viên khác.");

            trainer.Phone = phone;
        }

        if (!string.IsNullOrWhiteSpace(request.FullName)) trainer.FullName = request.FullName.Trim();
        if (request.Email != null) trainer.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (request.Gender != null) trainer.Gender = request.Gender;
        if (request.DateOfBirth.HasValue) trainer.DateOfBirth = request.DateOfBirth;
        if (request.Specialization != null) trainer.Specialization = request.Specialization.Trim();
        if (request.Bio != null) trainer.Bio = request.Bio.Trim();
        if (request.HourlyRate.HasValue) trainer.HourlyRate = request.HourlyRate.Value;
        if (request.AvatarUrl != null) trainer.AvatarUrl = request.AvatarUrl.Trim();
        if (request.BranchId.HasValue) trainer.BranchId = request.BranchId;
        if (request.IsActive.HasValue) trainer.IsActive = request.IsActive.Value;

        trainer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (trainer.BranchId.HasValue && trainer.Branch == null)
            await _context.Entry(trainer).Reference(t => t.Branch).LoadAsync();

        var classCount = await _context.TrainingClasses.CountAsync(c => c.TrainerId == trainerId);

        return MapToDto(trainer, classCount);
    }

    public async Task<TrainerDto> ChangeActiveAsync(Guid trainerId, bool isActive)
    {
        var trainer = await _context.Trainers
            .Include(t => t.Branch)
            .FirstOrDefaultAsync(t => t.TrainerId == trainerId)
            ?? throw new KeyNotFoundException("Không tìm thấy huấn luyện viên.");

        trainer.IsActive = isActive;
        trainer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var classCount = await _context.TrainingClasses.CountAsync(c => c.TrainerId == trainerId);

        return MapToDto(trainer, classCount);
    }

    public async Task DeleteTrainerAsync(Guid trainerId)
    {
        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.TrainerId == trainerId)
            ?? throw new KeyNotFoundException("Không tìm thấy huấn luyện viên.");

        // Gỡ huấn luyện viên khỏi các lớp/lịch học trước khi xóa
        var classes = await _context.TrainingClasses.Where(c => c.TrainerId == trainerId).ToListAsync();
        foreach (var trainingClass in classes)
        {
            trainingClass.TrainerId = null;
            trainingClass.UpdatedAt = DateTime.UtcNow;
        }

        var schedules = await _context.ClassSchedules.Where(s => s.TrainerId == trainerId).ToListAsync();
        foreach (var schedule in schedules)
        {
            schedule.TrainerId = null;
        }

        _context.Trainers.Remove(trainer);
        await _context.SaveChangesAsync();
    }

    private static TrainerDto MapToDto(Trainer trainer, int classesCount) => new()
    {
        TrainerId = trainer.TrainerId,
        TrainerCode = trainer.TrainerCode,
        FullName = trainer.FullName,
        Email = trainer.Email,
        Phone = trainer.Phone,
        Gender = trainer.Gender,
        DateOfBirth = trainer.DateOfBirth,
        Specialization = trainer.Specialization,
        Bio = trainer.Bio,
        HourlyRate = trainer.HourlyRate,
        JoinDate = trainer.JoinDate,
        AvatarUrl = trainer.AvatarUrl,
        UserId = trainer.UserId,
        BranchId = trainer.BranchId,
        BranchName = trainer.Branch?.BranchName,
        IsActive = trainer.IsActive,
        CreatedAt = trainer.CreatedAt,
        ClassesCount = classesCount
    };
}
