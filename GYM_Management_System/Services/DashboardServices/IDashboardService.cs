using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.DashboardDTOs;

namespace GYM_Management_System.Services.DashboardServices;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(Guid? branchId);

    Task<List<ExpiringSubscriptionDto>> GetExpiringSubscriptionsAsync(int days, Guid? branchId);
}
