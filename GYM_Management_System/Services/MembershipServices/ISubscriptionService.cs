using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.MembershipDTOs;

namespace GYM_Management_System.Services.MembershipServices;

public interface ISubscriptionService
{
    Task<PaginatedResponse<MemberSubscriptionDto>> GetSubscriptionsAsync(SubscriptionQueryParameters query);

    Task<MemberSubscriptionDto> GetSubscriptionByIdAsync(Guid subscriptionId);

    Task<MemberSubscriptionDto> GetActiveSubscriptionAsync(Guid memberId);

    Task<MemberSubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, Guid? currentUserId);

    Task<MemberSubscriptionDto> RenewSubscriptionAsync(Guid subscriptionId, RenewSubscriptionRequest request, Guid? currentUserId);

    Task<MemberSubscriptionDto> FreezeSubscriptionAsync(Guid subscriptionId, FreezeSubscriptionRequest request);

    Task<MemberSubscriptionDto> UnfreezeSubscriptionAsync(Guid subscriptionId);

    Task<MemberSubscriptionDto> CancelSubscriptionAsync(Guid subscriptionId, CancelSubscriptionRequest request);

    Task<List<ExpiringSubscriptionSummaryDto>> GetExpiringSubscriptionsAsync(int days);
}

/// <summary>Gói tập sắp hết hạn (dùng cho cảnh báo trên dashboard).</summary>
public class ExpiringSubscriptionSummaryDto
{
    public Guid SubscriptionId { get; set; }
    public string SubscriptionCode { get; set; } = "";
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = "";
    public string MemberPhone { get; set; } = "";
    public string PlanName { get; set; } = "";
    public DateTime EndDate { get; set; }
    public int DaysLeft { get; set; }
}
