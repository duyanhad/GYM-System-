using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GYM_Management_System.Services;

/// <summary>
/// Truy cập thông tin người dùng đang đăng nhập từ HttpContext (id, username, chi nhánh, IP).
/// </summary>
public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
    string? Username { get; }
    Guid? BranchId { get; }
    string? IpAddress { get; }
    bool IsInRole(string role);
}

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Username => Principal?.FindFirstValue(ClaimTypes.Name);

    public Guid? BranchId
    {
        get
        {
            var raw = Principal?.FindFirst("branchId")?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
