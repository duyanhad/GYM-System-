using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.MemberDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.MemberServices;

public class MemberService : IMemberService
{
    private readonly GymDbContext _context;

    public MemberService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<MemberDto>> GetMembersAsync(MemberQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var membersQuery = _context.Members
            .AsNoTracking()
            .Include(m => m.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            membersQuery = membersQuery.Where(m =>
                m.FullName.ToLower().Contains(keyword) ||
                m.MemberCode.ToLower().Contains(keyword) ||
                m.Phone.ToLower().Contains(keyword) ||
                (m.Email != null && m.Email.ToLower().Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
            membersQuery = membersQuery.Where(m => m.Status == query.Status);

        if (query.BranchId.HasValue)
            membersQuery = membersQuery.Where(m => m.BranchId == query.BranchId);

        membersQuery = (query.SortBy?.ToLower()) switch
        {
            "fullname" => query.SortDescending ? membersQuery.OrderByDescending(m => m.FullName) : membersQuery.OrderBy(m => m.FullName),
            "membercode" => query.SortDescending ? membersQuery.OrderByDescending(m => m.MemberCode) : membersQuery.OrderBy(m => m.MemberCode),
            "joindate" => query.SortDescending ? membersQuery.OrderByDescending(m => m.JoinDate) : membersQuery.OrderBy(m => m.JoinDate),
            _ => query.SortDescending ? membersQuery.OrderByDescending(m => m.CreatedAt) : membersQuery.OrderBy(m => m.CreatedAt)
        };

        var totalCount = await membersQuery.CountAsync();

        var members = await membersQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Gói tập đang hiệu lực của từng hội viên trong trang hiện tại
        var memberIds = members.Select(m => m.MemberId).ToList();
        var activeSubscriptions = await _context.MemberSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => memberIds.Contains(s.MemberId) && s.Status == DomainConstants.SubscriptionStatus.Active)
            .ToListAsync();

        var items = members.Select(m => MapToDto(m, activeSubscriptions)).ToList();

        return PaginatedResponse<MemberDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<MemberDetailDto> GetMemberByIdAsync(Guid memberId)
    {
        var member = await _context.Members
            .AsNoTracking()
            .Include(m => m.Branch)
            .FirstOrDefaultAsync(m => m.MemberId == memberId)
            ?? throw new KeyNotFoundException("Không tìm thấy hội viên.");

        var subscriptions = await _context.MemberSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => s.MemberId == memberId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();

        var activeSubscription = subscriptions.FirstOrDefault(s => s.Status == DomainConstants.SubscriptionStatus.Active);

        var detail = new MemberDetailDto
        {
            MemberId = member.MemberId,
            MemberCode = member.MemberCode,
            FullName = member.FullName,
            Email = member.Email,
            Phone = member.Phone,
            Gender = member.Gender,
            DateOfBirth = member.DateOfBirth,
            Age = CalculateAge(member.DateOfBirth),
            Address = member.Address,
            City = member.City,
            AvatarUrl = member.AvatarUrl,
            EmergencyContactName = member.EmergencyContactName,
            EmergencyContactPhone = member.EmergencyContactPhone,
            JoinDate = member.JoinDate,
            Status = member.Status,
            Notes = member.Notes,
            BranchId = member.BranchId,
            BranchName = member.Branch?.BranchName,
            CreatedAt = member.CreatedAt,
            UpdatedAt = member.UpdatedAt,
            CurrentPlanName = activeSubscription?.Plan?.PlanName,
            SubscriptionEndDate = activeSubscription?.EndDate,
            TotalCheckIns = await _context.CheckIns.CountAsync(c => c.MemberId == memberId),
            LastCheckInTime = await _context.CheckIns
                .Where(c => c.MemberId == memberId)
                .OrderByDescending(c => c.CheckInTime)
                .Select(c => (DateTime?)c.CheckInTime)
                .FirstOrDefaultAsync(),
            TotalPaid = await _context.Payments
                .Where(p => p.MemberId == memberId && p.Status == DomainConstants.PaymentStatus.Success)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            OutstandingBalance = await _context.Invoices
                .Where(i => i.MemberId == memberId && i.Status != DomainConstants.InvoiceStatus.Cancelled && i.Status != DomainConstants.InvoiceStatus.Paid)
                .SumAsync(i => (decimal?)(i.TotalAmount - i.PaidAmount)) ?? 0,
            Subscriptions = subscriptions.Select(s => new MemberSubscriptionHistoryDto
            {
                SubscriptionId = s.SubscriptionId,
                SubscriptionCode = s.SubscriptionCode,
                PlanName = s.Plan?.PlanName ?? "",
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status,
                FinalAmount = s.FinalAmount,
                PaidAmount = s.PaidAmount
            }).ToList()
        };

        return detail;
    }

    public async Task<MemberDto> CreateMemberAsync(CreateMemberRequest request)
    {
        var phone = request.Phone.Trim();

        var phoneExists = await _context.Members.AnyAsync(m => m.Phone == phone);
        if (phoneExists)
            throw new BusinessException("Số điện thoại này đã được đăng ký cho hội viên khác.");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();
            var emailExists = await _context.Members.AnyAsync(m => m.Email != null && m.Email.ToLower() == email.ToLower());
            if (emailExists)
                throw new BusinessException("Email này đã được đăng ký cho hội viên khác.");
        }

        if (request.BranchId.HasValue && !await _context.Branches.AnyAsync(b => b.BranchId == request.BranchId))
            throw new BusinessException("Chi nhánh không tồn tại.");

        var now = DateTime.UtcNow;
        var sequence = await _context.Members.CountAsync() + 1;

        var member = new Member
        {
            MemberId = Guid.NewGuid(),
            MemberCode = CodeGenerator.MemberCode(sequence),
            FullName = request.FullName.Trim(),
            Phone = phone,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            AvatarUrl = request.AvatarUrl?.Trim(),
            EmergencyContactName = request.EmergencyContactName?.Trim(),
            EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
            JoinDate = request.JoinDate ?? now,
            Status = DomainConstants.MemberStatus.Active,
            Notes = request.Notes,
            BranchId = request.BranchId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        if (member.BranchId.HasValue)
            await _context.Entry(member).Reference(m => m.Branch).LoadAsync();

        return MapToDto(member, new List<MemberSubscription>());
    }

    public async Task<MemberDto> UpdateMemberAsync(Guid memberId, UpdateMemberRequest request)
    {
        var member = await _context.Members
            .Include(m => m.Branch)
            .FirstOrDefaultAsync(m => m.MemberId == memberId)
            ?? throw new KeyNotFoundException("Không tìm thấy hội viên.");

        if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone.Trim() != member.Phone)
        {
            var phone = request.Phone.Trim();
            var phoneExists = await _context.Members.AnyAsync(m => m.MemberId != memberId && m.Phone == phone);
            if (phoneExists) throw new BusinessException("Số điện thoại này đã được đăng ký cho hội viên khác.");

            member.Phone = phone;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();
            var emailExists = await _context.Members
                .AnyAsync(m => m.MemberId != memberId && m.Email != null && m.Email.ToLower() == email.ToLower());
            if (emailExists) throw new BusinessException("Email này đã được đăng ký cho hội viên khác.");

            member.Email = email;
        }

        if (!string.IsNullOrWhiteSpace(request.FullName)) member.FullName = request.FullName.Trim();
        if (request.Gender != null) member.Gender = request.Gender;
        if (request.DateOfBirth.HasValue) member.DateOfBirth = request.DateOfBirth;
        if (request.Address != null) member.Address = request.Address.Trim();
        if (request.City != null) member.City = request.City.Trim();
        if (request.AvatarUrl != null) member.AvatarUrl = request.AvatarUrl.Trim();
        if (request.EmergencyContactName != null) member.EmergencyContactName = request.EmergencyContactName.Trim();
        if (request.EmergencyContactPhone != null) member.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
        if (request.Notes != null) member.Notes = request.Notes;
        if (request.BranchId.HasValue) member.BranchId = request.BranchId;

        member.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (member.BranchId.HasValue && member.Branch == null)
            await _context.Entry(member).Reference(m => m.Branch).LoadAsync();

        var activeSubscriptions = await _context.MemberSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => s.MemberId == memberId && s.Status == DomainConstants.SubscriptionStatus.Active)
            .ToListAsync();

        return MapToDto(member, activeSubscriptions);
    }

    public async Task<MemberDto> ChangeStatusAsync(Guid memberId, ChangeMemberStatusRequest request)
    {
        var validStatuses = new[]
        {
            DomainConstants.MemberStatus.Active,
            DomainConstants.MemberStatus.Inactive,
            DomainConstants.MemberStatus.Suspended
        };

        if (!validStatuses.Contains(request.Status))
            throw new BusinessException($"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}.");

        var member = await _context.Members
            .Include(m => m.Branch)
            .FirstOrDefaultAsync(m => m.MemberId == memberId)
            ?? throw new KeyNotFoundException("Không tìm thấy hội viên.");

        member.Status = request.Status;
        member.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var activeSubscriptions = await _context.MemberSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => s.MemberId == memberId && s.Status == DomainConstants.SubscriptionStatus.Active)
            .ToListAsync();

        return MapToDto(member, activeSubscriptions);
    }

    public async Task DeleteMemberAsync(Guid memberId)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.MemberId == memberId)
            ?? throw new KeyNotFoundException("Không tìm thấy hội viên.");

        var hasInvoices = await _context.Invoices.AnyAsync(i => i.MemberId == memberId);
        var hasSubscriptions = await _context.MemberSubscriptions.AnyAsync(s => s.MemberId == memberId);

        if (hasInvoices || hasSubscriptions)
            throw new BusinessException("Hội viên đã phát sinh gói tập/hóa đơn nên không thể xóa. Hãy chuyển sang trạng thái INACTIVE.");

        _context.Members.Remove(member);
        await _context.SaveChangesAsync();
    }

    public async Task<List<MemberSubscriptionHistoryDto>> GetSubscriptionHistoryAsync(Guid memberId)
    {
        var exists = await _context.Members.AnyAsync(m => m.MemberId == memberId);
        if (!exists) throw new KeyNotFoundException("Không tìm thấy hội viên.");

        return await _context.MemberSubscriptions
            .AsNoTracking()
            .Where(s => s.MemberId == memberId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new MemberSubscriptionHistoryDto
            {
                SubscriptionId = s.SubscriptionId,
                SubscriptionCode = s.SubscriptionCode,
                PlanName = s.Plan.PlanName,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status,
                FinalAmount = s.FinalAmount,
                PaidAmount = s.PaidAmount
            })
            .ToListAsync();
    }

    private static MemberDto MapToDto(Member member, List<MemberSubscription> activeSubscriptions)
    {
        var subscription = activeSubscriptions.FirstOrDefault(s => s.MemberId == member.MemberId);

        return new MemberDto
        {
            MemberId = member.MemberId,
            MemberCode = member.MemberCode,
            FullName = member.FullName,
            Email = member.Email,
            Phone = member.Phone,
            Gender = member.Gender,
            DateOfBirth = member.DateOfBirth,
            Age = CalculateAge(member.DateOfBirth),
            Address = member.Address,
            City = member.City,
            AvatarUrl = member.AvatarUrl,
            EmergencyContactName = member.EmergencyContactName,
            EmergencyContactPhone = member.EmergencyContactPhone,
            JoinDate = member.JoinDate,
            Status = member.Status,
            Notes = member.Notes,
            BranchId = member.BranchId,
            BranchName = member.Branch?.BranchName,
            CreatedAt = member.CreatedAt,
            UpdatedAt = member.UpdatedAt,
            CurrentPlanName = subscription?.Plan?.PlanName,
            SubscriptionEndDate = subscription?.EndDate
        };
    }

    private static int? CalculateAge(DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return null;

        var today = DateTime.UtcNow.Date;
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > today.AddYears(-age)) age--;

        return age;
    }
}
