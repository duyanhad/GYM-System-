using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.DashboardDTOs;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.DashboardServices;

public class DashboardService : IDashboardService
{
    private static readonly string[] VietnameseDayNames =
    {
        "Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy"
    };

    private readonly GymDbContext _context;

    public DashboardService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(Guid? branchId)
    {
        var now = DateTime.UtcNow;
        var monthStart = VietnamTime.MonthStartUtc;
        var previousMonthStart = VietnamTime.PreviousMonthStartUtc;
        var todayStart = VietnamTime.TodayStartUtc;
        var tomorrowStart = VietnamTime.TomorrowStartUtc;

        var members = _context.Members.AsNoTracking();
        var subscriptions = _context.MemberSubscriptions.AsNoTracking();
        var payments = _context.Payments.AsNoTracking();
        var checkIns = _context.CheckIns.AsNoTracking();
        var bookings = _context.ClassBookings.AsNoTracking();

        if (branchId.HasValue)
        {
            members = members.Where(m => m.BranchId == branchId);
            subscriptions = subscriptions.Where(s => s.BranchId == branchId);
            payments = payments.Where(p => p.BranchId == branchId);
            checkIns = checkIns.Where(c => c.BranchId == branchId);
            bookings = bookings.Where(b => b.TrainingClass.BranchId == branchId);
        }

        var revenueThisMonth = await payments
            .Where(p => p.Status == DomainConstants.PaymentStatus.Success && p.PaymentDate >= monthStart)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var revenueLastMonth = await payments
            .Where(p => p.Status == DomainConstants.PaymentStatus.Success
                        && p.PaymentDate >= previousMonthStart
                        && p.PaymentDate < monthStart)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var growth = revenueLastMonth > 0
            ? Math.Round((double)((revenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100, 1)
            : revenueThisMonth > 0 ? 100 : 0;

        var revenueChartRaw = await payments
            .Where(p => p.Status == DomainConstants.PaymentStatus.Success && p.PaymentDate >= monthStart.AddMonths(-5))
            .Select(p => new { p.PaymentDate, p.Amount })
            .ToListAsync();

        var revenueChart = revenueChartRaw
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new RevenueChartPointDto
            {
                Label = $"{g.Key.Month:D2}/{g.Key.Year}",
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                Revenue = g.Sum(x => x.Amount),
                PaymentCount = g.Count()
            })
            .ToList();

        var memberGrowthRaw = await members
            .Where(m => m.JoinDate >= monthStart.AddMonths(-5))
            .Select(m => m.JoinDate)
            .ToListAsync();

        var memberGrowth = memberGrowthRaw
            .GroupBy(d => new { d.Year, d.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MemberGrowthPointDto
            {
                Label = $"{g.Key.Month:D2}/{g.Key.Year}",
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                NewMembers = g.Count()
            })
            .ToList();

        var topClasses = await bookings
            .Where(b => b.Status != DomainConstants.BookingStatus.Cancelled && b.BookedAt >= monthStart)
            .GroupBy(b => new { b.ClassId, b.TrainingClass.ClassName })
            .Select(g => new TopClassDto
            {
                ClassId = g.Key.ClassId,
                ClassName = g.Key.ClassName,
                BookingCount = g.Count(),
                Revenue = g.Sum(x => x.TrainingClass.PricePerSession)
            })
            .OrderByDescending(x => x.BookingCount)
            .Take(5)
            .ToListAsync();

        var upcomingSchedules = await _context.ClassSchedules
            .AsNoTracking()
            .Include(s => s.TrainingClass)
            .ThenInclude(c => c!.Bookings)
            .Include(s => s.Trainer)
            .Where(s => s.IsActive && s.TrainingClass.IsActive
                        && (branchId == null || s.TrainingClass.BranchId == branchId))
            .ToListAsync();

        var upcomingClasses = upcomingSchedules
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Take(5)
            .Select(s => new UpcomingClassDto
            {
                ClassId = s.ClassId,
                ScheduleId = s.ScheduleId,
                ClassName = s.TrainingClass.ClassName,
                TrainerName = s.Trainer?.FullName,
                Room = s.Room,
                DayOfWeekName = VietnameseDayNames[(int)s.DayOfWeek],
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                BookedCount = s.Bookings.Count(b => b.Status == DomainConstants.BookingStatus.Booked),
                Capacity = s.TrainingClass.Capacity
            })
            .ToList();

        return new DashboardOverviewDto
        {
            TotalMembers = await members.CountAsync(),
            ActiveMembers = await members.CountAsync(m => m.Status == DomainConstants.MemberStatus.Active),
            NewMembersThisMonth = await members.CountAsync(m => m.JoinDate >= monthStart),
            ActiveSubscriptions = await subscriptions.CountAsync(s => s.Status == DomainConstants.SubscriptionStatus.Active),
            ExpiringSoonSubscriptions = await subscriptions.CountAsync(s => s.Status == DomainConstants.SubscriptionStatus.Active
                                                                            && s.EndDate >= now
                                                                            && s.EndDate <= now.AddDays(7)),
            TotalTrainers = await _context.Trainers.CountAsync(t => branchId == null || t.BranchId == branchId),
            TotalClasses = await _context.TrainingClasses.CountAsync(c => branchId == null || c.BranchId == branchId),
            TodayCheckIns = await checkIns.CountAsync(c => c.CheckInTime >= todayStart && c.CheckInTime < tomorrowStart),
            CurrentlyInside = await checkIns.CountAsync(c => c.CheckOutTime == null),
            TodayBookings = await bookings.CountAsync(b => b.BookedAt >= todayStart && b.BookedAt < tomorrowStart),
            RevenueThisMonth = revenueThisMonth,
            RevenueLastMonth = revenueLastMonth,
            RevenueGrowthPercent = growth,
            OutstandingAmount = await _context.Invoices
                .AsNoTracking()
                .Where(i => i.Status != DomainConstants.InvoiceStatus.Cancelled
                            && i.Status != DomainConstants.InvoiceStatus.Paid
                            && (branchId == null || i.BranchId == branchId))
                .SumAsync(i => (decimal?)(i.TotalAmount - i.PaidAmount)) ?? 0,
            RevenueChart = revenueChart,
            MemberGrowthChart = memberGrowth,
            TopClasses = topClasses,
            UpcomingClasses = upcomingClasses
        };
    }

    public async Task<List<ExpiringSubscriptionDto>> GetExpiringSubscriptionsAsync(int days, Guid? branchId)
    {
        var threshold = DateTime.UtcNow.AddDays(days <= 0 ? 7 : days);

        var query = _context.MemberSubscriptions
            .AsNoTracking()
            .Include(s => s.Member)
            .Include(s => s.Plan)
            .Where(s => s.Status == DomainConstants.SubscriptionStatus.Active
                        && s.EndDate >= DateTime.UtcNow
                        && s.EndDate <= threshold);

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId);

        return await query
            .OrderBy(s => s.EndDate)
            .Select(s => new ExpiringSubscriptionDto
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
}
