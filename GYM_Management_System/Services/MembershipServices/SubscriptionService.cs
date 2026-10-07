using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.BillingDTOs;
using GYM_Management_System.DTOs.MembershipDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using GYM_Management_System.Services.BillingServices;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.MembershipServices;

public class SubscriptionService : ISubscriptionService
{
    private readonly GymDbContext _context;
    private readonly IInvoiceService _invoiceService;

    public SubscriptionService(GymDbContext context, IInvoiceService invoiceService)
    {
        _context = context;
        _invoiceService = invoiceService;
    }

    public async Task<PaginatedResponse<MemberSubscriptionDto>> GetSubscriptionsAsync(SubscriptionQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        await RefreshExpiredSubscriptionsAsync();

        var subscriptionsQuery = _context.MemberSubscriptions
            .AsNoTracking()
            .Include(s => s.Member)
            .Include(s => s.Plan)
            .Include(s => s.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            subscriptionsQuery = subscriptionsQuery.Where(s =>
                s.SubscriptionCode.ToLower().Contains(keyword) ||
                s.Member.FullName.ToLower().Contains(keyword) ||
                s.Member.MemberCode.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
            subscriptionsQuery = subscriptionsQuery.Where(s => s.Status == query.Status);

        if (query.MemberId.HasValue)
            subscriptionsQuery = subscriptionsQuery.Where(s => s.MemberId == query.MemberId);

        if (query.PlanId.HasValue)
            subscriptionsQuery = subscriptionsQuery.Where(s => s.PlanId == query.PlanId);

        if (query.BranchId.HasValue)
            subscriptionsQuery = subscriptionsQuery.Where(s => s.BranchId == query.BranchId);

        if (query.FromDate.HasValue)
            subscriptionsQuery = subscriptionsQuery.Where(s => s.StartDate >= query.FromDate);

        if (query.ToDate.HasValue)
            subscriptionsQuery = subscriptionsQuery.Where(s => s.EndDate <= query.ToDate);

        var totalCount = await subscriptionsQuery.CountAsync();

        var subscriptions = await subscriptionsQuery
            .OrderByDescending(s => s.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = subscriptions.Select(MapToDto).ToList();

        return PaginatedResponse<MemberSubscriptionDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<MemberSubscriptionDto> GetSubscriptionByIdAsync(Guid subscriptionId)
    {
        var subscription = await _context.MemberSubscriptions
            .AsNoTracking()
            .Include(s => s.Member)
            .Include(s => s.Plan)
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId)
            ?? throw new KeyNotFoundException("Không tìm thấy đăng ký gói tập.");

        return MapToDto(subscription);
    }

    public async Task<MemberSubscriptionDto> GetActiveSubscriptionAsync(Guid memberId)
    {
        var subscription = await _context.MemberSubscriptions
            .AsNoTracking()
            .Include(s => s.Member)
            .Include(s => s.Plan)
            .Include(s => s.Branch)
            .Where(s => s.MemberId == memberId && s.Status == DomainConstants.SubscriptionStatus.Active)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Hội viên hiện không có gói tập nào đang hiệu lực.");

        return MapToDto(subscription);
    }

    public async Task<MemberSubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, Guid? currentUserId)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.MemberId == request.MemberId)
            ?? throw new KeyNotFoundException("Không tìm thấy hội viên.");

        var plan = await _context.MembershipPlans.FirstOrDefaultAsync(p => p.PlanId == request.PlanId)
            ?? throw new KeyNotFoundException("Không tìm thấy gói tập.");

        if (!plan.IsActive)
            throw new BusinessException("Gói tập này đã ngừng bán.");

        if (member.Status != DomainConstants.MemberStatus.Active)
            throw new BusinessException("Hội viên đang không ở trạng thái hoạt động. Vui lòng kiểm tra lại.");

        var existingActive = await _context.MemberSubscriptions
            .AnyAsync(s => s.MemberId == request.MemberId && s.Status == DomainConstants.SubscriptionStatus.Active);

        if (existingActive)
            throw new BusinessException("Hội viên đang có gói tập hiệu lực. Hãy dùng chức năng gia hạn thay vì tạo mới.");

        var startDate = (request.StartDate ?? VietnamTime.TodayStartUtc).Date;
        var endDate = startDate.AddDays(plan.DurationDays);

        if (request.DiscountAmount < 0 || request.DiscountAmount > plan.Price)
            throw new BusinessException("Số tiền giảm giá không hợp lệ.");

        var now = DateTime.UtcNow;
        var branchId = request.BranchId ?? member.BranchId ?? plan.BranchId;
        var sequence = await _context.MemberSubscriptions.CountAsync() + 1;

        var subscription = new MemberSubscription
        {
            SubscriptionId = Guid.NewGuid(),
            SubscriptionCode = CodeGenerator.SubscriptionCode(now, sequence),
            MemberId = member.MemberId,
            PlanId = plan.PlanId,
            BranchId = branchId,
            StartDate = startDate,
            EndDate = endDate,
            Status = DomainConstants.SubscriptionStatus.Active,
            TotalPrice = plan.Price,
            DiscountAmount = request.DiscountAmount,
            FinalAmount = plan.Price - request.DiscountAmount,
            PaidAmount = 0,
            PaymentStatus = DomainConstants.PaymentStatus.Unpaid,
            RemainingPersonalTrainingSessions = plan.PersonalTrainingSessions ?? 0,
            Notes = request.Notes,
            CreatedBy = currentUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.MemberSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        await _context.Entry(subscription).Reference(s => s.Member).LoadAsync();
        await _context.Entry(subscription).Reference(s => s.Plan).LoadAsync();
        if (subscription.BranchId.HasValue)
            await _context.Entry(subscription).Reference(s => s.Branch).LoadAsync();

        if (request.CreateInvoice)
        {
            await _invoiceService.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                MemberId = member.MemberId,
                BranchId = branchId,
                SubscriptionId = subscription.SubscriptionId,
                DiscountAmount = request.DiscountAmount,
                Notes = $"Hóa đơn đăng ký gói tập {plan.PlanName}",
                Items = new List<CreateInvoiceItemRequest>
                {
                    new()
                    {
                        ItemType = DomainConstants.InvoiceItemType.Subscription,
                        ReferenceId = plan.PlanId,
                        ItemName = plan.PlanName,
                        Quantity = 1,
                        UnitPrice = plan.Price
                    }
                }
            }, currentUserId);
        }

        return MapToDto(subscription);
    }

    public async Task<MemberSubscriptionDto> RenewSubscriptionAsync(
        Guid subscriptionId,
        RenewSubscriptionRequest request,
        Guid? currentUserId)
    {
        var current = await _context.MemberSubscriptions
            .Include(s => s.Plan)
            .Include(s => s.Member)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId)
            ?? throw new KeyNotFoundException("Không tìm thấy đăng ký gói tập.");

        var plan = request.PlanId.HasValue
            ? await _context.MembershipPlans.FirstOrDefaultAsync(p => p.PlanId == request.PlanId)
                ?? throw new KeyNotFoundException("Không tìm thấy gói tập cần gia hạn.")
            : current.Plan;

        if (!plan.IsActive)
            throw new BusinessException("Gói tập này đã ngừng bán.");

        if (request.DiscountAmount < 0 || request.DiscountAmount > plan.Price)
            throw new BusinessException("Số tiền giảm giá không hợp lệ.");

        var now = DateTime.UtcNow;
        var baseDate = (request.StartDate ?? (current.EndDate > now ? current.EndDate : VietnamTime.TodayStartUtc)).Date;
        var sequence = await _context.MemberSubscriptions.CountAsync() + 1;

        var renewed = new MemberSubscription
        {
            SubscriptionId = Guid.NewGuid(),
            SubscriptionCode = CodeGenerator.SubscriptionCode(now, sequence),
            MemberId = current.MemberId,
            PlanId = plan.PlanId,
            BranchId = current.BranchId,
            StartDate = baseDate,
            EndDate = baseDate.AddDays(plan.DurationDays),
            Status = DomainConstants.SubscriptionStatus.Active,
            TotalPrice = plan.Price,
            DiscountAmount = request.DiscountAmount,
            FinalAmount = plan.Price - request.DiscountAmount,
            PaidAmount = 0,
            PaymentStatus = DomainConstants.PaymentStatus.Unpaid,
            RemainingPersonalTrainingSessions = plan.PersonalTrainingSessions ?? 0,
            Notes = request.Notes ?? $"Gia hạn từ {current.SubscriptionCode}",
            CreatedBy = currentUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Gói cũ chuyển sang EXPIRED nếu đã hết hạn, hoặc giữ nguyên nếu còn hiệu lực (gói mới nối tiếp)
        if (current.EndDate <= now)
            current.Status = DomainConstants.SubscriptionStatus.Expired;

        current.UpdatedAt = now;

        _context.MemberSubscriptions.Add(renewed);
        await _context.SaveChangesAsync();

        await _context.Entry(renewed).Reference(s => s.Plan).LoadAsync();
        await _context.Entry(renewed).Reference(s => s.Member).LoadAsync();
        if (renewed.BranchId.HasValue)
            await _context.Entry(renewed).Reference(s => s.Branch).LoadAsync();

        if (request.CreateInvoice)
        {
            await _invoiceService.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                MemberId = renewed.MemberId,
                BranchId = renewed.BranchId,
                SubscriptionId = renewed.SubscriptionId,
                DiscountAmount = request.DiscountAmount,
                Notes = $"Hóa đơn gia hạn gói tập {plan.PlanName}",
                Items = new List<CreateInvoiceItemRequest>
                {
                    new()
                    {
                        ItemType = DomainConstants.InvoiceItemType.Subscription,
                        ReferenceId = plan.PlanId,
                        ItemName = plan.PlanName,
                        Quantity = 1,
                        UnitPrice = plan.Price
                    }
                }
            }, currentUserId);
        }

        return MapToDto(renewed);
    }

    public async Task<MemberSubscriptionDto> FreezeSubscriptionAsync(Guid subscriptionId, FreezeSubscriptionRequest request)
    {
        var subscription = await _context.MemberSubscriptions
            .Include(s => s.Plan)
            .Include(s => s.Member)
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId)
            ?? throw new KeyNotFoundException("Không tìm thấy đăng ký gói tập.");

        if (subscription.Status != DomainConstants.SubscriptionStatus.Active)
            throw new BusinessException("Chỉ có thể bảo lưu gói tập đang hiệu lực.");

        if (subscription.Plan.MaxFreezeDays <= 0)
            throw new BusinessException("Gói tập này không hỗ trợ bảo lưu.");

        if (request.Days > subscription.Plan.MaxFreezeDays)
            throw new BusinessException($"Số ngày bảo lưu vượt quá quy định của gói ({subscription.Plan.MaxFreezeDays} ngày).");

        var now = DateTime.UtcNow;

        subscription.Status = DomainConstants.SubscriptionStatus.Frozen;
        subscription.FreezeStartDate = now;
        subscription.FreezeEndDate = now.AddDays(request.Days);
        subscription.EndDate = subscription.EndDate.AddDays(request.Days);
        subscription.Notes = string.IsNullOrWhiteSpace(request.Reason)
            ? subscription.Notes
            : $"{subscription.Notes}\n[Bảo lưu {request.Days} ngày] {request.Reason}".Trim();
        subscription.UpdatedAt = now;

        await _context.SaveChangesAsync();

        return MapToDto(subscription);
    }

    public async Task<MemberSubscriptionDto> UnfreezeSubscriptionAsync(Guid subscriptionId)
    {
        var subscription = await _context.MemberSubscriptions
            .Include(s => s.Plan)
            .Include(s => s.Member)
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId)
            ?? throw new KeyNotFoundException("Không tìm thấy đăng ký gói tập.");

        if (subscription.Status != DomainConstants.SubscriptionStatus.Frozen)
            throw new BusinessException("Gói tập này hiện không ở trạng thái bảo lưu.");

        var now = DateTime.UtcNow;

        // Nếu mở bảo lưu sớm hơn dự kiến thì chỉ cộng bù số ngày thực tế đã bảo lưu
        var plannedDays = subscription.FreezeEndDate.HasValue && subscription.FreezeStartDate.HasValue
            ? (subscription.FreezeEndDate.Value - subscription.FreezeStartDate.Value).TotalDays
            : 0;
        var actualDays = subscription.FreezeStartDate.HasValue
            ? (now - subscription.FreezeStartDate.Value).TotalDays
            : 0;

        if (plannedDays > actualDays && actualDays >= 0)
            subscription.EndDate = subscription.EndDate.AddDays(-(plannedDays - actualDays));

        subscription.Status = subscription.EndDate <= now
            ? DomainConstants.SubscriptionStatus.Expired
            : DomainConstants.SubscriptionStatus.Active;

        subscription.FreezeStartDate = null;
        subscription.FreezeEndDate = null;
        subscription.UpdatedAt = now;

        await _context.SaveChangesAsync();

        return MapToDto(subscription);
    }

    public async Task<MemberSubscriptionDto> CancelSubscriptionAsync(Guid subscriptionId, CancelSubscriptionRequest request)
    {
        var subscription = await _context.MemberSubscriptions
            .Include(s => s.Plan)
            .Include(s => s.Member)
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId)
            ?? throw new KeyNotFoundException("Không tìm thấy đăng ký gói tập.");

        if (subscription.Status is DomainConstants.SubscriptionStatus.Cancelled or DomainConstants.SubscriptionStatus.Expired)
            throw new BusinessException("Gói tập này đã kết thúc, không thể hủy.");

        subscription.Status = DomainConstants.SubscriptionStatus.Cancelled;
        subscription.Notes = string.IsNullOrWhiteSpace(request.Reason)
            ? subscription.Notes
            : $"{subscription.Notes}\n[Đã hủy] {request.Reason}".Trim();
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(subscription);
    }

    public async Task<List<ExpiringSubscriptionSummaryDto>> GetExpiringSubscriptionsAsync(int days)
    {
        var threshold = DateTime.UtcNow.AddDays(days <= 0 ? 7 : days);

        return await _context.MemberSubscriptions
            .AsNoTracking()
            .Include(s => s.Member)
            .Include(s => s.Plan)
            .Where(s => s.Status == DomainConstants.SubscriptionStatus.Active
                        && s.EndDate >= DateTime.UtcNow
                        && s.EndDate <= threshold)
            .OrderBy(s => s.EndDate)
            .Select(s => new ExpiringSubscriptionSummaryDto
            {
                SubscriptionId = s.SubscriptionId,
                SubscriptionCode = s.SubscriptionCode,
                MemberId = s.MemberId,
                MemberName = s.Member.FullName,
                MemberPhone = s.Member.Phone,
                PlanName = s.Plan.PlanName,
                EndDate = s.EndDate,
                DaysLeft = (int)(s.EndDate - DateTime.UtcNow).TotalDays
            })
            .ToListAsync();
    }

    /// <summary>
    /// Đồng bộ trạng thái các gói tập đã quá hạn nhưng chưa được cập nhật.
    /// </summary>
    private async Task RefreshExpiredSubscriptionsAsync()
    {
        var now = DateTime.UtcNow;

        var expired = await _context.MemberSubscriptions
            .Where(s => s.Status == DomainConstants.SubscriptionStatus.Active && s.EndDate < now)
            .ToListAsync();

        if (expired.Count == 0) return;

        foreach (var subscription in expired)
        {
            subscription.Status = DomainConstants.SubscriptionStatus.Expired;
            subscription.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();
    }

    private static MemberSubscriptionDto MapToDto(MemberSubscription subscription)
    {
        var remainingDays = (subscription.EndDate - DateTime.UtcNow).Days;

        return new MemberSubscriptionDto
        {
            SubscriptionId = subscription.SubscriptionId,
            SubscriptionCode = subscription.SubscriptionCode,
            MemberId = subscription.MemberId,
            MemberName = subscription.Member?.FullName,
            MemberCode = subscription.Member?.MemberCode,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan?.PlanName,
            BranchId = subscription.BranchId,
            BranchName = subscription.Branch?.BranchName,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status,
            TotalPrice = subscription.TotalPrice,
            DiscountAmount = subscription.DiscountAmount,
            FinalAmount = subscription.FinalAmount,
            PaidAmount = subscription.PaidAmount,
            RemainingAmount = subscription.FinalAmount - subscription.PaidAmount,
            PaymentStatus = subscription.PaymentStatus,
            RemainingPersonalTrainingSessions = subscription.RemainingPersonalTrainingSessions,
            FreezeStartDate = subscription.FreezeStartDate,
            FreezeEndDate = subscription.FreezeEndDate,
            RemainingDays = remainingDays > 0 ? remainingDays : 0,
            Notes = subscription.Notes,
            CreatedAt = subscription.CreatedAt
        };
    }
}
