using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.BranchDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.BranchServices;

public class BranchService : IBranchService
{
    private readonly GymDbContext _context;

    public BranchService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<List<BranchDto>> GetBranchesAsync(bool? isActive)
    {
        var query = _context.Branches
            .AsNoTracking()
            .Include(b => b.ManagerUser)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive);

        var branches = await query.OrderBy(b => b.BranchName).ToListAsync();

        var memberCounts = await _context.Members
            .AsNoTracking()
            .Where(m => m.BranchId != null)
            .GroupBy(m => m.BranchId!.Value)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BranchId, x => x.Count);

        var subscriptionCounts = await _context.MemberSubscriptions
            .AsNoTracking()
            .Where(s => s.BranchId != null && s.Status == DomainConstants.SubscriptionStatus.Active)
            .GroupBy(s => s.BranchId!.Value)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BranchId, x => x.Count);

        var trainerCounts = await _context.Trainers
            .AsNoTracking()
            .Where(t => t.BranchId != null)
            .GroupBy(t => t.BranchId!.Value)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BranchId, x => x.Count);

        return branches.Select(b => MapToDto(
            b,
            memberCounts.GetValueOrDefault(b.BranchId),
            subscriptionCounts.GetValueOrDefault(b.BranchId),
            trainerCounts.GetValueOrDefault(b.BranchId))).ToList();
    }

    public async Task<BranchDto> GetBranchByIdAsync(Guid branchId)
    {
        var branch = await _context.Branches
            .AsNoTracking()
            .Include(b => b.ManagerUser)
            .FirstOrDefaultAsync(b => b.BranchId == branchId)
            ?? throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

        var memberCount = await _context.Members.CountAsync(m => m.BranchId == branchId);
        var subscriptionCount = await _context.MemberSubscriptions
            .CountAsync(s => s.BranchId == branchId && s.Status == DomainConstants.SubscriptionStatus.Active);
        var trainerCount = await _context.Trainers.CountAsync(t => t.BranchId == branchId);

        return MapToDto(branch, memberCount, subscriptionCount, trainerCount);
    }

    public async Task<BranchDto> CreateBranchAsync(CreateBranchRequest request)
    {
        var branchName = request.BranchName.Trim();

        if (await _context.Branches.AnyAsync(b => b.BranchName.ToLower() == branchName.ToLower()))
            throw new BusinessException("Tên chi nhánh đã tồn tại.");

        var now = DateTime.UtcNow;
        var branchCode = request.BranchCode?.Trim();
        if (string.IsNullOrWhiteSpace(branchCode))
        {
            var sequence = await _context.Branches.CountAsync() + 1;
            branchCode = CodeGenerator.BranchCode(sequence);
        }
        else if (await _context.Branches.AnyAsync(b => b.BranchCode == branchCode))
        {
            throw new BusinessException("Mã chi nhánh đã tồn tại.");
        }

        if (request.ManagerUserId.HasValue && !await _context.Users.AnyAsync(u => u.UserId == request.ManagerUserId))
            throw new BusinessException("Người quản lý không tồn tại.");

        var branch = new Branch
        {
            BranchId = Guid.NewGuid(),
            BranchName = branchName,
            BranchCode = branchCode,
            Phone = request.Phone?.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            OpeningTime = request.OpeningTime,
            ClosingTime = request.ClosingTime,
            ManagerUserId = request.ManagerUserId,
            IsActive = true,
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        if (branch.ManagerUserId.HasValue)
            await _context.Entry(branch).Reference(b => b.ManagerUser).LoadAsync();

        return MapToDto(branch, 0, 0, 0);
    }

    public async Task<BranchDto> UpdateBranchAsync(Guid branchId, UpdateBranchRequest request)
    {
        var branch = await _context.Branches
            .Include(b => b.ManagerUser)
            .FirstOrDefaultAsync(b => b.BranchId == branchId)
            ?? throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

        if (!string.IsNullOrWhiteSpace(request.BranchName) && request.BranchName.Trim() != branch.BranchName)
        {
            var branchName = request.BranchName.Trim();
            if (await _context.Branches.AnyAsync(b => b.BranchId != branchId && b.BranchName.ToLower() == branchName.ToLower()))
                throw new BusinessException("Tên chi nhánh đã tồn tại.");

            branch.BranchName = branchName;
        }

        if (!string.IsNullOrWhiteSpace(request.BranchCode) && request.BranchCode.Trim() != branch.BranchCode)
        {
            var branchCode = request.BranchCode.Trim();
            if (await _context.Branches.AnyAsync(b => b.BranchId != branchId && b.BranchCode == branchCode))
                throw new BusinessException("Mã chi nhánh đã tồn tại.");

            branch.BranchCode = branchCode;
        }

        if (request.Phone != null) branch.Phone = request.Phone.Trim();
        if (request.Email != null) branch.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (request.Address != null) branch.Address = request.Address.Trim();
        if (request.City != null) branch.City = request.City.Trim();
        if (request.OpeningTime.HasValue) branch.OpeningTime = request.OpeningTime;
        if (request.ClosingTime.HasValue) branch.ClosingTime = request.ClosingTime;
        if (request.ManagerUserId.HasValue) branch.ManagerUserId = request.ManagerUserId;
        if (request.IsActive.HasValue) branch.IsActive = request.IsActive.Value;
        if (request.Notes != null) branch.Notes = request.Notes;

        branch.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (branch.ManagerUserId.HasValue && branch.ManagerUser == null)
            await _context.Entry(branch).Reference(b => b.ManagerUser).LoadAsync();

        var memberCount = await _context.Members.CountAsync(m => m.BranchId == branchId);
        var subscriptionCount = await _context.MemberSubscriptions
            .CountAsync(s => s.BranchId == branchId && s.Status == DomainConstants.SubscriptionStatus.Active);
        var trainerCount = await _context.Trainers.CountAsync(t => t.BranchId == branchId);

        return MapToDto(branch, memberCount, subscriptionCount, trainerCount);
    }

    public async Task DeleteBranchAsync(Guid branchId)
    {
        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.BranchId == branchId)
            ?? throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

        var hasData = await _context.Members.AnyAsync(m => m.BranchId == branchId)
                      || await _context.Invoices.AnyAsync(i => i.BranchId == branchId)
                      || await _context.TrainingClasses.AnyAsync(c => c.BranchId == branchId);

        if (hasData)
            throw new BusinessException("Chi nhánh đã có dữ liệu (hội viên/hóa đơn/lớp học) nên không thể xóa. Hãy chuyển sang trạng thái ngừng hoạt động.");

        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync();
    }

    private static BranchDto MapToDto(Branch branch, int totalMembers, int activeSubscriptions, int totalTrainers) => new()
    {
        BranchId = branch.BranchId,
        BranchName = branch.BranchName,
        BranchCode = branch.BranchCode,
        Phone = branch.Phone,
        Email = branch.Email,
        Address = branch.Address,
        City = branch.City,
        OpeningTime = branch.OpeningTime,
        ClosingTime = branch.ClosingTime,
        ManagerUserId = branch.ManagerUserId,
        ManagerName = branch.ManagerUser?.FullName,
        IsActive = branch.IsActive,
        Notes = branch.Notes,
        CreatedAt = branch.CreatedAt,
        UpdatedAt = branch.UpdatedAt,
        TotalMembers = totalMembers,
        ActiveSubscriptions = activeSubscriptions,
        TotalTrainers = totalTrainers
    };
}
