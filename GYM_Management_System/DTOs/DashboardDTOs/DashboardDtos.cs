using System;
using System.Collections.Generic;

namespace GYM_Management_System.DTOs.DashboardDTOs;

/// <summary>
/// Số liệu tổng quan hiển thị trên dashboard.
/// </summary>
public class DashboardOverviewDto
{
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public int NewMembersThisMonth { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int ExpiringSoonSubscriptions { get; set; }
    public int TotalTrainers { get; set; }
    public int TotalClasses { get; set; }
    public int TodayCheckIns { get; set; }
    public int CurrentlyInside { get; set; }
    public int TodayBookings { get; set; }

    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueLastMonth { get; set; }
    public double RevenueGrowthPercent { get; set; }
    public decimal OutstandingAmount { get; set; }

    public List<RevenueChartPointDto> RevenueChart { get; set; } = new();
    public List<MemberGrowthPointDto> MemberGrowthChart { get; set; } = new();
    public List<TopClassDto> TopClasses { get; set; } = new();
    public List<UpcomingClassDto> UpcomingClasses { get; set; } = new();
}

public class RevenueChartPointDto
{
    public string Label { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int PaymentCount { get; set; }
}

public class MemberGrowthPointDto
{
    public string Label { get; set; } = "";
    public DateTime Date { get; set; }
    public int NewMembers { get; set; }
}

public class TopClassDto
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = "";
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
}

public class UpcomingClassDto
{
    public Guid ClassId { get; set; }
    public Guid ScheduleId { get; set; }
    public string ClassName { get; set; } = "";
    public string? TrainerName { get; set; }
    public string? Room { get; set; }
    public string DayOfWeekName { get; set; } = "";
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BookedCount { get; set; }
    public int Capacity { get; set; }
}

public class ExpiringSubscriptionDto
{
    public Guid SubscriptionId { get; set; }
    public string SubscriptionCode { get; set; } = "";
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = "";
    public string MemberPhone { get; set; } = "";
    public string PlanName { get; set; } = "";
    public DateTime EndDate { get; set; }
    public int DaysLeft { get; set; }
}
