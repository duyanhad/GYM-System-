using System;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.Services.DashboardServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.DashboardController;

/// <summary>
/// Dashboard tổng quan: hội viên, doanh thu, lịch học, lưu lượng ra vào.
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HasPermission(PermissionConstants.DASHBOARD_VIEW)]
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] Guid? branchId)
    {
        var result = await _dashboardService.GetOverviewAsync(branchId);
        return Ok(result);
    }

    [HasPermission(PermissionConstants.DASHBOARD_VIEW)]
    [HttpGet("expiring-subscriptions")]
    public async Task<IActionResult> GetExpiringSubscriptions([FromQuery] int days = 7, [FromQuery] Guid? branchId = null)
    {
        var result = await _dashboardService.GetExpiringSubscriptionsAsync(days, branchId);
        return Ok(result);
    }
}
