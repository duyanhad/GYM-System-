using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.WorkoutDTOs;

namespace GYM_Management_System.Services.WorkoutServices;

public interface IWorkoutShareService
{
    Task<List<ConnectionDto>> GetConnectionsAsync(Guid userId);

    Task<ConnectionDto> InviteConnectionAsync(Guid userId, InviteConnectionRequest request);

    Task<ConnectionDto> AcceptConnectionAsync(Guid userId, Guid connectionId);

    Task<WorkoutPlanShareDto> SharePlanAsync(Guid userId, ShareWorkoutPlanRequest request);

    Task<List<WorkoutPlanShareDto>> GetReceivedSharesAsync(Guid userId);

    Task<List<WorkoutPlanShareDto>> GetSentSharesAsync(Guid userId);

    /// <summary>Nhận giáo án được chia sẻ thành giáo án của mình.</summary>
    Task<WorkoutPlanDto> ImportSharedPlanAsync(Guid userId, Guid shareId);

    Task<List<UserNotificationDto>> GetNotificationsAsync(Guid userId, bool unreadOnly);

    Task<int> MarkNotificationsReadAsync(Guid userId, Guid? notificationId);
}
