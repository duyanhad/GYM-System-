using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.DTOs.SystemDTOs;
using GYM_Management_System.Models;
using GYM_Management_System.Services.SystemServices;
using Microsoft.AspNetCore.Mvc;

namespace GYM_Management_System.Controllers.SystemController;

/// <summary>
/// Nhật ký hoạt động hệ thống (audit log).
/// </summary>
[ApiController]
[Route("api/system-logs")]
public class SystemLogsController : ControllerBase
{
    private readonly ISystemLogService _logService;

    public SystemLogsController(ISystemLogService logService)
    {
        _logService = logService;
    }

    [HasPermission(PermissionConstants.SYSTEM_LOG_VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] SystemLogQueryParameters query)
    {
        var result = await _logService.GetLogsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Ghi log thao tác quan trọng từ phía client (vd: in hóa đơn, xuất báo cáo...).
    /// </summary>
    [HasPermission(PermissionConstants.SYSTEM_MANAGE)]
    [HttpPost]
    public async Task<IActionResult> WriteLog([FromBody] WriteSystemLogRequest request)
    {
        await _logService.WriteAsync(
            request.Action,
            request.EntityName,
            request.EntityId,
            request.Description,
            DomainConstants.LogLevel.Info);

        return Ok(new { success = true, message = "Ghi nhật ký thành công." });
    }
}

/// <summary>Yêu cầu ghi nhật ký từ client.</summary>
public class WriteSystemLogRequest
{
    public string Action { get; set; } = "";
    public string? EntityName { get; set; }
    public System.Guid? EntityId { get; set; }
    public string? Description { get; set; }
}
