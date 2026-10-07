using System;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.SystemDTOs;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.SystemServices;

public class SystemLogService : ISystemLogService
{
    private readonly GymDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;

    public SystemLogService(GymDbContext context, ICurrentUserAccessor currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task WriteAsync(
        string action,
        string? entityName = null,
        Guid? entityId = null,
        string? description = null,
        string? level = null,
        Guid? userId = null,
        string? username = null)
    {
        _context.SystemLogs.Add(new SystemLog
        {
            LogId = Guid.NewGuid(),
            UserId = userId ?? _currentUser.UserId,
            Username = username ?? _currentUser.Username,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            Level = level ?? DomainConstants.LogLevel.Info,
            IpAddress = _currentUser.IpAddress,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<SystemLogDto>> GetLogsAsync(SystemLogQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var logsQuery = _context.SystemLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            logsQuery = logsQuery.Where(l =>
                l.Action.ToLower().Contains(keyword) ||
                (l.Description != null && l.Description.ToLower().Contains(keyword)) ||
                (l.Username != null && l.Username.ToLower().Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(query.Level)) logsQuery = logsQuery.Where(l => l.Level == query.Level);
        if (!string.IsNullOrWhiteSpace(query.EntityName)) logsQuery = logsQuery.Where(l => l.EntityName == query.EntityName);
        if (query.UserId.HasValue) logsQuery = logsQuery.Where(l => l.UserId == query.UserId);
        if (query.FromDate.HasValue) logsQuery = logsQuery.Where(l => l.CreatedAt >= query.FromDate);
        if (query.ToDate.HasValue) logsQuery = logsQuery.Where(l => l.CreatedAt <= query.ToDate);

        var totalCount = await logsQuery.CountAsync();

        var logs = await logsQuery
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new SystemLogDto
            {
                LogId = l.LogId,
                UserId = l.UserId,
                Username = l.Username,
                Action = l.Action,
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                Description = l.Description,
                Level = l.Level,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return PaginatedResponse<SystemLogDto>.Create(logs, totalCount, pageNumber, pageSize);
    }
}
