using System.Threading.Tasks;

namespace GYM_Management_System.Services;

/// <summary>
/// Trừu tượng hóa việc gửi thông báo realtime để service nghiệp vụ không phụ thuộc trực tiếp SignalR.
/// </summary>
public interface INotificationSender
{
    Task SendToAllAsync(string eventName, object payload);

    Task SendToBranchAsync(string branchId, string eventName, object payload);

    Task SendToUserAsync(string userId, string eventName, object payload);
}
