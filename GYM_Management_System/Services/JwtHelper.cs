using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GYM_Management_System.Services;

/// <summary>
/// Sinh và đọc token JWT (kèm claim role + permission để phục vụ RBAC).
/// </summary>
public class JwtHelper
{
    private readonly IConfiguration _config;

    public JwtHelper(IConfiguration config)
    {
        _config = config;
    }

    public DateTime GetExpiryTime()
        => DateTime.UtcNow.AddMinutes(GetExpiryMinutes());

    private double GetExpiryMinutes()
    {
        var raw = _config["Jwt:ExpiryInMinutes"];
        return double.TryParse(raw, out var minutes) && minutes > 0 ? minutes : 1440;
    }

    /// <summary>
    /// Sinh token cho user. <paramref name="permissions"/> được nạp vào claim "permission".
    /// </summary>
    public string GenerateToken(
        Guid userId,
        string username,
        string fullName,
        List<string> roles,
        List<string> permissions,
        Guid? defaultBranchId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.GivenName, fullName)
        };

        if (defaultBranchId.HasValue && defaultBranchId.Value != Guid.Empty)
        {
            claims.Add(new Claim("branchId", defaultBranchId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: GetExpiryTime(),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateAndReadToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

        try
        {
            return tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch
        {
            return null;
        }
    }
}
