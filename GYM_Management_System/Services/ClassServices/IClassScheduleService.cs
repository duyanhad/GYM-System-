using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.ClassDTOs;

namespace GYM_Management_System.Services.ClassServices;

public interface IClassScheduleService
{
    Task<List<ClassScheduleDto>> GetSchedulesAsync(Guid? classId, Guid? trainerId, int? dayOfWeek, bool? isActive);

    Task<ClassScheduleDto> GetScheduleByIdAsync(Guid scheduleId);

    Task<List<WeeklyScheduleDto>> GetWeeklyTimetableAsync(Guid? branchId);

    Task<ClassScheduleDto> CreateScheduleAsync(CreateClassScheduleRequest request);

    Task<ClassScheduleDto> UpdateScheduleAsync(Guid scheduleId, UpdateClassScheduleRequest request);

    Task DeleteScheduleAsync(Guid scheduleId);
}
