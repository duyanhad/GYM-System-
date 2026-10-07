using System;
using System.ComponentModel.DataAnnotations;

namespace GYM_Management_System.DTOs.BranchDTOs;

public class BranchDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public string? BranchCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public TimeSpan? OpeningTime { get; set; }
    public TimeSpan? ClosingTime { get; set; }
    public Guid? ManagerUserId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int TotalMembers { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int TotalTrainers { get; set; }
}

public class CreateBranchRequest
{
    [Required(ErrorMessage = "Tên chi nhánh là bắt buộc.")]
    [MaxLength(200)]
    public string BranchName { get; set; } = "";

    [MaxLength(50)]
    public string? BranchCode { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string? Email { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public TimeSpan? OpeningTime { get; set; }
    public TimeSpan? ClosingTime { get; set; }
    public Guid? ManagerUserId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateBranchRequest
{
    [MaxLength(200)]
    public string? BranchName { get; set; }

    [MaxLength(50)]
    public string? BranchCode { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string? Email { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public TimeSpan? OpeningTime { get; set; }
    public TimeSpan? ClosingTime { get; set; }
    public Guid? ManagerUserId { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}
