using System;
using System.ComponentModel.DataAnnotations;

namespace GYM_Management_System.DTOs.TrainerDTOs;

public class TrainerDto
{
    public Guid TrainerId { get; set; }
    public string TrainerCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string Phone { get; set; } = "";
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Specialization { get; set; }
    public string? Bio { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime JoinDate { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? UserId { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }

    /// <summary>Số lớp đang phụ trách.</summary>
    public int ClassesCount { get; set; }
}

public class CreateTrainerRequest
{
    [Required(ErrorMessage = "Họ tên huấn luyện viên là bắt buộc.")]
    [MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [MaxLength(30)]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string? Email { get; set; }

    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Specialization { get; set; }
    public string? Bio { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime? JoinDate { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? BranchId { get; set; }
}

public class UpdateTrainerRequest
{
    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string? Email { get; set; }

    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Specialization { get; set; }
    public string? Bio { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? BranchId { get; set; }
    public bool? IsActive { get; set; }
}

public class TrainerQueryParameters
{
    public string? Search { get; set; }
    public string? Specialization { get; set; }
    public Guid? BranchId { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
