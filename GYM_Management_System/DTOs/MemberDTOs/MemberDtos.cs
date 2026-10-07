using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GYM_Management_System.DTOs.MemberDTOs;

public class MemberDto
{
    public Guid MemberId { get; set; }
    public string MemberCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string Phone { get; set; } = "";
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? AvatarUrl { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime JoinDate { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gói tập đang còn hiệu lực (nếu có).</summary>
    public string? CurrentPlanName { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
}

public class CreateMemberRequest
{
    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    [MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [MaxLength(30)]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string? Email { get; set; }

    /// <summary>MALE / FEMALE / OTHER.</summary>
    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? AvatarUrl { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime? JoinDate { get; set; }
    public string? Notes { get; set; }
    public Guid? BranchId { get; set; }
}

public class UpdateMemberRequest
{
    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string? Email { get; set; }

    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? AvatarUrl { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Notes { get; set; }
    public Guid? BranchId { get; set; }
}

public class ChangeMemberStatusRequest
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
    public string Status { get; set; } = "";
}

public class MemberQueryParameters
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public Guid? BranchId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>fullName | memberCode | joinDate | createdAt</summary>
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

public class MemberSubscriptionHistoryDto
{
    public Guid SubscriptionId { get; set; }
    public string SubscriptionCode { get; set; } = "";
    public string PlanName { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "";
    public decimal FinalAmount { get; set; }
    public decimal PaidAmount { get; set; }
}

public class MemberDetailDto : MemberDto
{
    public List<MemberSubscriptionHistoryDto> Subscriptions { get; set; } = new();
    public int TotalCheckIns { get; set; }
    public DateTime? LastCheckInTime { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
}
