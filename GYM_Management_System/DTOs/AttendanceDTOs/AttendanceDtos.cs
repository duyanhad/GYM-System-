using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GYM_Management_System.DTOs.AttendanceDTOs;

public class CheckInDto
{
    public Guid CheckInId { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberCode { get; set; }
    public string? MemberPhone { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string Method { get; set; } = "";
    public string? Notes { get; set; }
}

public class CreateCheckInRequest
{
    /// <summary>Mã hội viên (quét QR / nhập tay). Có thể dùng MemberId thay thế.</summary>
    public string? MemberCode { get; set; }

    public Guid? MemberId { get; set; }

    public Guid? BranchId { get; set; }

    /// <summary>QR / CODE / MANUAL.</summary>
    public string Method { get; set; } = Models.DomainConstants.CheckInMethod.Manual;

    public string? Notes { get; set; }
}

public class CheckOutRequest
{
    public Guid CheckInId { get; set; }
    public string? Notes { get; set; }
}

public class AttendanceQueryParameters
{
    public string? Search { get; set; }
    public Guid? MemberId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    /// <summary>Chỉ lấy các lượt chưa check-out.</summary>
    public bool? StillInside { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AttendanceStatsDto
{
    public DateTime Date { get; set; }
    public int TotalCheckIns { get; set; }
    public int CurrentlyInside { get; set; }
    public int UniqueMembers { get; set; }
    public double AverageDurationMinutes { get; set; }
    public List<HourlyAttendanceDto> Hourly { get; set; } = new();
}

public class HourlyAttendanceDto
{
    public int Hour { get; set; }
    public int CheckIns { get; set; }
}
