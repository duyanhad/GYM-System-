using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace GYM_Management_System.Hubs;

/// <summary>
/// SignalR hub dùng để đẩy thông báo realtime cho FE (check-in mới, hóa đơn mới, thông báo hệ thống...).
/// </summary>
public class GymHub : Hub
{
    /// <summary>FE gọi để tham gia nhóm theo chi nhánh và nhận thông báo của chi nhánh đó.</summary>
    public async Task JoinBranch(string branchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, BranchGroup(branchId));
    }

    public async Task LeaveBranch(string branchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, BranchGroup(branchId));
    }

    public static string BranchGroup(string branchId) => $"branch-{branchId}";
}
