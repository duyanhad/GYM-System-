using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.SystemDTOs;

namespace GYM_Management_System.Services.SystemServices;

public interface ISystemLogService
{
    Task WriteAsync(string action, string? entityName = null, Guid? entityId = null, string? description = null,
        string? level = null, Guid? userId = null, string? username = null);

    Task<PaginatedResponse<SystemLogDto>> GetLogsAsync(SystemLogQueryParameters query);
}
