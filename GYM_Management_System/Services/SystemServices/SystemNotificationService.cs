using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.SystemDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.SystemServices;

public class SystemNotificationService : ISystemNotificationService
{
    private readonly GymDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly INotificationSender _notificationSender;

    public SystemNotificationService(
        GymDbContext context,
        ICurrentUserAccessor currentUser,
        INotificationSender notificationSender)
    {
        _context = context;
        _currentUser = currentUser;
        _notificationSender = notificationSender;
    }

    public async Task<List<SystemNotificationDto>> GetNotificationsAsync(bool unreadOnly, int take)
    {
        var userId = _currentUser.UserId;
        var branchId = _currentUser.BranchId;
        var limit = take is < 1 or > 200 ? 50 : take;

        var query = _context.SystemNotifications
            .AsNoTracking()
            .Where(n => n.UserId == null || n.UserId == userId
                        || (branchId != null && n.BranchId == branchId));

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new SystemNotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                UserId = n.UserId,
                BranchId = n.BranchId,
                ReferenceType = n.ReferenceType,
                ReferenceId = n.ReferenceId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SystemNotificationDto> CreateAsync(CreateNotificationRequest request)
    {
        var notification = new SystemNotification
        {
            NotificationId = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Type = request.Type,
            UserId = request.UserId,
            BranchId = request.BranchId,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.SystemNotifications.Add(notification);
        await _context.SaveChangesAsync();

        var dto = MapToDto(notification);

        if (notification.UserId.HasValue)
            await _notificationSender.SendToUserAsync(notification.UserId.Value.ToString(), "notification", dto);
        else if (notification.BranchId.HasValue)
            await _notificationSender.SendToBranchAsync(notification.BranchId.Value.ToString(), "notification", dto);
        else
            await _notificationSender.SendToAllAsync("notification", dto);

        return dto;
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _context.SystemNotifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId)
            ?? throw new KeyNotFoundException("Không tìm thấy thông báo.");

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> MarkAllAsReadAsync()
    {
        var userId = _currentUser.UserId;
        var branchId = _currentUser.BranchId;

        var notifications = await _context.SystemNotifications
            .Where(n => !n.IsRead && (n.UserId == null || n.UserId == userId
                                      || (branchId != null && n.BranchId == branchId)))
            .ToListAsync();

        foreach (var notification in notifications)
            notification.IsRead = true;

        await _context.SaveChangesAsync();

        return notifications.Count;
    }

    private static SystemNotificationDto MapToDto(SystemNotification notification) => new()
    {
        NotificationId = notification.NotificationId,
        Title = notification.Title,
        Message = notification.Message,
        Type = notification.Type,
        UserId = notification.UserId,
        BranchId = notification.BranchId,
        ReferenceType = notification.ReferenceType,
        ReferenceId = notification.ReferenceId,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt
    };
}
