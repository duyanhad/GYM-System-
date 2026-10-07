using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.MembershipDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.MembershipServices;

public class MembershipPlanService : IMembershipPlanService
{
    private readonly GymDbContext _context;

    public MembershipPlanService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<MembershipPlanDto>> GetPlansAsync(MembershipPlanQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var plansQuery = _context.MembershipPlans
            .AsNoTracking()
            .Include(p => p.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            plansQuery = plansQuery.Where(p =>
                p.PlanName.ToLower().Contains(keyword) ||
                p.PlanCode.ToLower().Contains(keyword));
        }

        if (query.IsActive.HasValue)
            plansQuery = plansQuery.Where(p => p.IsActive == query.IsActive);

        if (query.BranchId.HasValue)
            plansQuery = plansQuery.Where(p => p.BranchId == query.BranchId);

        var totalCount = await plansQuery.CountAsync();

        var plans = await plansQuery
            .OrderBy(p => p.Price)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var planIds = plans.Select(p => p.PlanId).ToList();
        var activeCounts = await _context.MemberSubscriptions
            .AsNoTracking()
            .Where(s => planIds.Contains(s.PlanId) && s.Status == DomainConstants.SubscriptionStatus.Active)
            .GroupBy(s => s.PlanId)
            .Select(g => new { PlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Count);

        var items = plans.Select(p => MapToDto(p, activeCounts.GetValueOrDefault(p.PlanId))).ToList();

        return PaginatedResponse<MembershipPlanDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<MembershipPlanDto> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _context.MembershipPlans
            .AsNoTracking()
            .Include(p => p.Branch)
            .FirstOrDefaultAsync(p => p.PlanId == planId)
            ?? throw new KeyNotFoundException("Không tìm thấy gói tập.");

        var activeCount = await _context.MemberSubscriptions
            .CountAsync(s => s.PlanId == planId && s.Status == DomainConstants.SubscriptionStatus.Active);

        return MapToDto(plan, activeCount);
    }

    public async Task<MembershipPlanDto> CreatePlanAsync(CreateMembershipPlanRequest request)
    {
        var planCode = request.PlanCode.Trim();

        if (await _context.MembershipPlans.AnyAsync(p => p.PlanCode.ToLower() == planCode.ToLower()))
            throw new BusinessException("Mã gói tập đã tồn tại.");

        if (request.BranchId.HasValue && !await _context.Branches.AnyAsync(b => b.BranchId == request.BranchId))
            throw new BusinessException("Chi nhánh không tồn tại.");

        var now = DateTime.UtcNow;

        var plan = new MembershipPlan
        {
            PlanId = Guid.NewGuid(),
            PlanCode = planCode,
            PlanName = request.PlanName.Trim(),
            Description = request.Description,
            DurationDays = request.DurationDays,
            Price = request.Price,
            PersonalTrainingSessions = request.PersonalTrainingSessions,
            MaxFreezeDays = request.MaxFreezeDays,
            BranchId = request.BranchId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.MembershipPlans.Add(plan);
        await _context.SaveChangesAsync();

        if (plan.BranchId.HasValue)
            await _context.Entry(plan).Reference(p => p.Branch).LoadAsync();

        return MapToDto(plan, 0);
    }

    public async Task<MembershipPlanDto> UpdatePlanAsync(Guid planId, UpdateMembershipPlanRequest request)
    {
        var plan = await _context.MembershipPlans
            .Include(p => p.Branch)
            .FirstOrDefaultAsync(p => p.PlanId == planId)
            ?? throw new KeyNotFoundException("Không tìm thấy gói tập.");

        if (!string.IsNullOrWhiteSpace(request.PlanName)) plan.PlanName = request.PlanName.Trim();
        if (request.Description != null) plan.Description = request.Description;
        if (request.DurationDays.HasValue && request.DurationDays.Value > 0) plan.DurationDays = request.DurationDays.Value;
        if (request.Price.HasValue && request.Price.Value >= 0) plan.Price = request.Price.Value;
        if (request.PersonalTrainingSessions.HasValue) plan.PersonalTrainingSessions = request.PersonalTrainingSessions;
        if (request.MaxFreezeDays.HasValue && request.MaxFreezeDays.Value >= 0) plan.MaxFreezeDays = request.MaxFreezeDays.Value;
        if (request.BranchId.HasValue) plan.BranchId = request.BranchId;
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;

        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (plan.BranchId.HasValue && plan.Branch == null)
            await _context.Entry(plan).Reference(p => p.Branch).LoadAsync();

        var activeCount = await _context.MemberSubscriptions
            .CountAsync(s => s.PlanId == planId && s.Status == DomainConstants.SubscriptionStatus.Active);

        return MapToDto(plan, activeCount);
    }

    public async Task<MembershipPlanDto> ChangeActiveAsync(Guid planId, bool isActive)
    {
        var plan = await _context.MembershipPlans
            .Include(p => p.Branch)
            .FirstOrDefaultAsync(p => p.PlanId == planId)
            ?? throw new KeyNotFoundException("Không tìm thấy gói tập.");

        plan.IsActive = isActive;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var activeCount = await _context.MemberSubscriptions
            .CountAsync(s => s.PlanId == planId && s.Status == DomainConstants.SubscriptionStatus.Active);

        return MapToDto(plan, activeCount);
    }

    public async Task DeletePlanAsync(Guid planId)
    {
        var plan = await _context.MembershipPlans.FirstOrDefaultAsync(p => p.PlanId == planId)
            ?? throw new KeyNotFoundException("Không tìm thấy gói tập.");

        var hasSubscriptions = await _context.MemberSubscriptions.AnyAsync(s => s.PlanId == planId);
        if (hasSubscriptions)
            throw new BusinessException("Gói tập đã có hội viên đăng ký nên không thể xóa. Hãy chuyển gói sang trạng thái ngừng bán.");

        _context.MembershipPlans.Remove(plan);
        await _context.SaveChangesAsync();
    }

    private static MembershipPlanDto MapToDto(MembershipPlan plan, int activeSubscriptions) => new()
    {
        PlanId = plan.PlanId,
        PlanCode = plan.PlanCode,
        PlanName = plan.PlanName,
        Description = plan.Description,
        DurationDays = plan.DurationDays,
        Price = plan.Price,
        PersonalTrainingSessions = plan.PersonalTrainingSessions,
        MaxFreezeDays = plan.MaxFreezeDays,
        BranchId = plan.BranchId,
        BranchName = plan.Branch?.BranchName,
        IsActive = plan.IsActive,
        CreatedAt = plan.CreatedAt,
        UpdatedAt = plan.UpdatedAt,
        ActiveSubscriptions = activeSubscriptions
    };
}
