using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.Services;

namespace GYM_Management_System.UnitTests.Fakes;

/// <summary>
/// Bản giả của INotificationSender dùng trong unit test (không cần SignalR).
/// </summary>
public class FakeNotificationSender : INotificationSender
{
    public List<(string Event, object Payload)> Broadcasts { get; } = new();
    public List<(string BranchId, string Event, object Payload)> BranchMessages { get; } = new();
    public List<(string UserId, string Event, object Payload)> UserMessages { get; } = new();

    public Task SendToAllAsync(string eventName, object payload)
    {
        Broadcasts.Add((eventName, payload));
        return Task.CompletedTask;
    }

    public Task SendToBranchAsync(string branchId, string eventName, object payload)
    {
        BranchMessages.Add((branchId, eventName, payload));
        return Task.CompletedTask;
    }

    public Task SendToUserAsync(string userId, string eventName, object payload)
    {
        UserMessages.Add((userId, eventName, payload));
        return Task.CompletedTask;
    }
}
