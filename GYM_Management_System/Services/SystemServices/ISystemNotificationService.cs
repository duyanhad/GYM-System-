using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.SystemDTOs;

namespace GYM_Management_System.Services.SystemServices;

public interface ISystemNotificationService
{
    Task<List<SystemNotificationDto>> GetNotificationsAsync(bool unreadOnly, int take);

    Task<SystemNotificationDto> CreateAsync(CreateNotificationRequest request);

    Task<bool> MarkAsReadAsync(Guid notificationId);

    Task<int> MarkAllAsReadAsync();
}
