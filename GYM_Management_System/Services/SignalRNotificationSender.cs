using System.Threading.Tasks;
using GYM_Management_System.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GYM_Management_System.Services;

/// <summary>
/// Gửi thông báo realtime qua SignalR (GymHub).
/// </summary>
public class SignalRNotificationSender : INotificationSender
{
    private readonly IHubContext<GymHub> _hubContext;

    public SignalRNotificationSender(IHubContext<GymHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendToAllAsync(string eventName, object payload)
        => _hubContext.Clients.All.SendAsync(eventName, payload);

    public Task SendToBranchAsync(string branchId, string eventName, object payload)
        => _hubContext.Clients.Group(GymHub.BranchGroup(branchId)).SendAsync(eventName, payload);

    public Task SendToUserAsync(string userId, string eventName, object payload)
        => _hubContext.Clients.User(userId).SendAsync(eventName, payload);
}
