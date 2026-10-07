using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.WorkoutDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.WorkoutServices;

/// <summary>
/// Kết nối tài khoản và chia sẻ giáo án giữa những tài khoản đã kết nối.
/// </summary>
public class WorkoutShareService : IWorkoutShareService
{
    private readonly GymDbContext _context;

    public WorkoutShareService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<List<ConnectionDto>> GetConnectionsAsync(Guid userId)
    {
        var connections = await _context.UserConnections
            .AsNoTracking()
            .Where(c => c.RequesterUserId == userId || c.AddresseeUserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var otherIds = connections
            .Select(c => c.RequesterUserId == userId ? c.AddresseeUserId : c.RequesterUserId)
            .Distinct()
            .ToList();

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => otherIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.Username, u.FullName })
            .ToDictionaryAsync(u => u.UserId);

        return connections.Select(connection =>
        {
            var isRequester = connection.RequesterUserId == userId;
            var otherId = isRequester ? connection.AddresseeUserId : connection.RequesterUserId;
            users.TryGetValue(otherId, out var other);

            return new ConnectionDto
            {
                UserConnectionId = connection.UserConnectionId,
                UserId = otherId,
                Username = other?.Username ?? "",
                FullName = other?.FullName ?? "",
                Status = connection.Status,
                Direction = isRequester ? "SENT" : "RECEIVED"
            };
        }).ToList();
    }

    public async Task<ConnectionDto> InviteConnectionAsync(Guid userId, InviteConnectionRequest request)
    {
        var query = request.Query.Trim().ToLower();

        var target = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == query || u.Email.ToLower() == query)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản với tên đăng nhập/email này.");

        if (target.UserId == userId)
            throw new BusinessException("Bạn không thể tự kết nối với chính mình.");

        var existing = await _context.UserConnections
            .FirstOrDefaultAsync(c =>
                (c.RequesterUserId == userId && c.AddresseeUserId == target.UserId)
                || (c.RequesterUserId == target.UserId && c.AddresseeUserId == userId));

        if (existing != null)
        {
            if (existing.Status == DomainConstants.ConnectionStatus.Accepted)
                throw new BusinessException("Bạn đã kết nối với tài khoản này rồi.");

            if (existing.RequestStatusIsPending() && existing.RequesterUserId == userId)
                throw new BusinessException("Bạn đã gửi lời mời kết nối tới tài khoản này.");

            // Người kia đã mời trước đó => chấp nhận luôn
            existing.Status = DomainConstants.ConnectionStatus.Accepted;
            existing.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (await GetConnectionsAsync(userId))
                .First(c => c.UserConnectionId == existing.UserConnectionId);
        }

        var now = DateTime.UtcNow;

        var connection = new UserConnection
        {
            UserConnectionId = Guid.NewGuid(),
            RequesterUserId = userId,
            AddresseeUserId = target.UserId,
            Status = DomainConstants.ConnectionStatus.Pending,
            CreatedAt = now
        };

        _context.UserConnections.Add(connection);

        _context.UserNotifications.Add(new UserNotification
        {
            UserNotificationId = Guid.NewGuid(),
            UserId = target.UserId,
            Title = "Lời mời kết nối mới",
            Message = "Bạn có một lời mời kết nối để cùng chia sẻ giáo án tập luyện.",
            Type = "CONNECTION_REQUEST",
            ReferenceId = connection.UserConnectionId,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        return (await GetConnectionsAsync(userId))
            .First(c => c.UserConnectionId == connection.UserConnectionId);
    }

    public async Task<ConnectionDto> AcceptConnectionAsync(Guid userId, Guid connectionId)
    {
        var connection = await _context.UserConnections
            .FirstOrDefaultAsync(c => c.UserConnectionId == connectionId)
            ?? throw new KeyNotFoundException("Không tìm thấy lời mời kết nối.");

        if (connection.AddresseeUserId != userId)
            throw new BusinessException("Chỉ người nhận mới có thể chấp nhận lời mời này.");

        connection.Status = DomainConstants.ConnectionStatus.Accepted;
        connection.RespondedAt = DateTime.UtcNow;

        _context.UserNotifications.Add(new UserNotification
        {
            UserNotificationId = Guid.NewGuid(),
            UserId = connection.RequesterUserId,
            Title = "Lời mời kết nối đã được chấp nhận",
            Message = "Bây giờ bạn có thể chia sẻ giáo án tập luyện cho tài khoản này.",
            Type = "CONNECTION_ACCEPTED",
            ReferenceId = connection.UserConnectionId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return (await GetConnectionsAsync(userId))
            .First(c => c.UserConnectionId == connection.UserConnectionId);
    }

    public async Task<WorkoutPlanShareDto> SharePlanAsync(Guid userId, ShareWorkoutPlanRequest request)
    {
        var plan = await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.WorkoutPlanId == request.WorkoutPlanId && (p.UserId == null || p.UserId == userId))
            ?? throw new KeyNotFoundException("Không tìm thấy buổi tập cần chia sẻ.");

        var toUserId = await ResolveTargetUserIdAsync(userId, request);

        var now = DateTime.UtcNow;

        var share = new WorkoutPlanShare
        {
            WorkoutPlanShareId = Guid.NewGuid(),
            WorkoutPlanId = plan.WorkoutPlanId,
            FromUserId = userId,
            ToUserId = toUserId,
            Message = request.Message?.Trim(),
            SharedAt = now
        };

        _context.WorkoutPlanShares.Add(share);

        _context.UserNotifications.Add(new UserNotification
        {
            UserNotificationId = Guid.NewGuid(),
            UserId = toUserId,
            Title = "Bạn được chia sẻ một giáo án",
            Message = $"Giáo án \"{plan.Name}\" vừa được chia sẻ cho bạn.",
            Type = "PLAN_SHARED",
            ReferenceId = plan.WorkoutPlanId,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        var sender = await _context.Users.AsNoTracking().FirstAsync(u => u.UserId == userId);
        var receiver = await _context.Users.AsNoTracking().FirstAsync(u => u.UserId == toUserId);

        return new WorkoutPlanShareDto
        {
            WorkoutPlanShareId = share.WorkoutPlanShareId,
            WorkoutPlanId = plan.WorkoutPlanId,
            PlanName = plan.Name,
            FromUserId = userId,
            FromUserName = sender.FullName,
            ToUserId = toUserId,
            ToUserName = receiver.FullName,
            Message = share.Message,
            SharedAt = share.SharedAt
        };
    }

    public Task<List<WorkoutPlanShareDto>> GetReceivedSharesAsync(Guid userId)
        => QuerySharesAsync(shares => shares.Where(s => s.ToUserId == userId), isReceived: true);

    public Task<List<WorkoutPlanShareDto>> GetSentSharesAsync(Guid userId)
        => QuerySharesAsync(shares => shares.Where(s => s.FromUserId == userId), isReceived: false);

    public async Task<WorkoutPlanDto> ImportSharedPlanAsync(Guid userId, Guid shareId)
    {
        var share = await _context.WorkoutPlanShares
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkoutPlanShareId == shareId && s.ToUserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy giáo án được chia sẻ.");

        var source = await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.WorkoutPlanId == share.WorkoutPlanId)
            ?? throw new KeyNotFoundException("Giáo án gốc không còn tồn tại.");

        var now = DateTime.UtcNow;
        var copy = new WorkoutPlan
        {
            WorkoutPlanId = Guid.NewGuid(),
            UserId = userId,
            Name = $"{source.Name} (nhận)",
            Focus = source.Focus,
            Note = source.Note,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var order = 1;

        foreach (var item in source.Items.OrderBy(i => i.OrderIndex))
        {
            copy.Items.Add(new WorkoutPlanItem
            {
                WorkoutPlanItemId = Guid.NewGuid(),
                WorkoutPlanId = copy.WorkoutPlanId,
                OrderIndex = order++,
                ExerciseId = item.ExerciseId,
                ExerciseName = item.ExerciseName,
                TargetSets = item.TargetSets,
                TargetReps = item.TargetReps,
                RestSeconds = item.RestSeconds,
                Note = item.Note
            });
        }

        _context.WorkoutPlans.Add(copy);
        await _context.SaveChangesAsync();

        return new WorkoutPlanDto
        {
            WorkoutPlanId = copy.WorkoutPlanId,
            Name = copy.Name,
            Focus = copy.Focus,
            Note = copy.Note,
            IsSystem = false,
            CreatedAt = copy.CreatedAt,
            TotalSets = copy.Items.Sum(i => i.TargetSets),
            Items = copy.Items.Select(item => new WorkoutPlanItemDto
            {
                WorkoutPlanItemId = item.WorkoutPlanItemId,
                OrderIndex = item.OrderIndex,
                ExerciseId = item.ExerciseId,
                ExerciseName = item.ExerciseName,
                TargetSets = item.TargetSets,
                TargetReps = item.TargetReps,
                RestSeconds = item.RestSeconds,
                Note = item.Note
            }).ToList()
        };
    }

    public async Task<List<UserNotificationDto>> GetNotificationsAsync(Guid userId, bool unreadOnly)
    {
        var query = _context.UserNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly) query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new UserNotificationDto
            {
                UserNotificationId = n.UserNotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                ReferenceId = n.ReferenceId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<int> MarkNotificationsReadAsync(Guid userId, Guid? notificationId)
    {
        var query = _context.UserNotifications.Where(n => n.UserId == userId && !n.IsRead);

        if (notificationId.HasValue) query = query.Where(n => n.UserNotificationId == notificationId.Value);

        var notifications = await query.ToListAsync();

        foreach (var notification in notifications)
            notification.IsRead = true;

        await _context.SaveChangesAsync();

        return notifications.Count;
    }

    private async Task<List<WorkoutPlanShareDto>> QuerySharesAsync(
        Func<IQueryable<WorkoutPlanShare>, IQueryable<WorkoutPlanShare>> filter,
        bool isReceived)
    {
        var shares = await filter(_context.WorkoutPlanShares.AsNoTracking())
            .OrderByDescending(s => s.SharedAt)
            .Take(100)
            .ToListAsync();

        var planIds = shares.Select(s => s.WorkoutPlanId).Distinct().ToList();
        var userIds = shares.Select(s => s.FromUserId).Concat(shares.Select(s => s.ToUserId)).Distinct().ToList();

        var plans = await _context.WorkoutPlans
            .AsNoTracking()
            .Where(p => planIds.Contains(p.WorkoutPlanId))
            .ToDictionaryAsync(p => p.WorkoutPlanId, p => p.Name);

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => u.FullName);

        return shares.Select(share => new WorkoutPlanShareDto
        {
            WorkoutPlanShareId = share.WorkoutPlanShareId,
            WorkoutPlanId = share.WorkoutPlanId,
            PlanName = plans.GetValueOrDefault(share.WorkoutPlanId, "Giáo án"),
            FromUserId = share.FromUserId,
            FromUserName = users.GetValueOrDefault(share.FromUserId, "Người dùng"),
            ToUserId = share.ToUserId,
            ToUserName = users.GetValueOrDefault(share.ToUserId, "Người dùng"),
            Message = share.Message,
            SharedAt = share.SharedAt,
            IsReceived = isReceived
        }).ToList();
    }

    private async Task<Guid> ResolveTargetUserIdAsync(Guid userId, ShareWorkoutPlanRequest request)
    {
        if (request.ToUserId.HasValue) return await EnsureConnectedAsync(userId, request.ToUserId.Value);

        if (request.UserConnectionId.HasValue)
        {
            var connection = await _context.UserConnections
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserConnectionId == request.UserConnectionId.Value)
                ?? throw new KeyNotFoundException("Không tìm thấy kết nối.");

            if (connection.Status != DomainConstants.ConnectionStatus.Accepted)
                throw new BusinessException("Hai tài khoản chưa kết nối nên chưa thể chia sẻ.");

            var otherId = connection.RequesterUserId == userId ? connection.AddresseeUserId : connection.RequesterUserId;

            if (otherId == userId) throw new BusinessException("Kết nối không hợp lệ.");

            return otherId;
        }

        throw new BusinessException("Cần chọn tài khoản nhận giáo án.");
    }

    private async Task<Guid> EnsureConnectedAsync(Guid userId, Guid targetUserId)
    {
        if (targetUserId == userId) throw new BusinessException("Không thể chia sẻ cho chính mình.");

        var connected = await _context.UserConnections.AnyAsync(c =>
            c.Status == DomainConstants.ConnectionStatus.Accepted
            && ((c.RequesterUserId == userId && c.AddresseeUserId == targetUserId)
                || (c.RequesterUserId == targetUserId && c.AddresseeUserId == userId)));

        if (!connected)
            throw new BusinessException("Hai tài khoản chưa kết nối nên chưa thể chia sẻ giáo án.");

        return targetUserId;
    }
}

/// <summary>Tiện ích nhỏ cho trạng thái lời mời kết nối.</summary>
internal static class UserConnectionExtensions
{
    public static bool RequestStatusIsPending(this UserConnection connection)
        => connection.Status == DomainConstants.ConnectionStatus.Pending;
}
