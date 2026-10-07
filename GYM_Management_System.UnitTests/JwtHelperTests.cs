using System;
using System.Collections.Generic;
using GYM_Management_System.Services;
using Microsoft.Extensions.Configuration;

namespace GYM_Management_System.UnitTests;

public class JwtHelperTests
{
    private static JwtHelper CreateHelper() => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "UNIT_TEST_KEY_0123456789_0123456789_0123456789",
            ["Jwt:Issuer"] = "GymManagementSystemAPI",
            ["Jwt:Audience"] = "GymManagementSystemUsers",
            ["Jwt:ExpiryInMinutes"] = "60"
        })
        .Build());

    [Fact]
    public void GenerateToken_ShouldContainRolesAndPermissions()
    {
        var helper = CreateHelper();
        var userId = Guid.NewGuid();

        var token = helper.GenerateToken(
            userId,
            "manager",
            "Nguyễn Văn Quản Lý",
            new List<string> { "Manager" },
            new List<string> { "MEMBER_VIEW", "PAYMENT_CREATE" },
            Guid.NewGuid());

        var principal = helper.ValidateAndReadToken(token);

        Assert.NotNull(principal);
        Assert.Contains(principal!.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Manager");
        Assert.Contains(principal.Claims, c => c.Type == "permission" && c.Value == "MEMBER_VIEW");
        Assert.Contains(principal.Claims, c => c.Type == "branchId");
        Assert.Equal(userId.ToString(), principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void ValidateAndReadToken_WithTamperedToken_ShouldReturnNull()
    {
        var helper = CreateHelper();
        var token = helper.GenerateToken(Guid.NewGuid(), "admin", "Admin", new List<string> { "Admin" }, new List<string>());

        var principal = helper.ValidateAndReadToken(token + "tampered");

        Assert.Null(principal);
    }

    [Fact]
    public void GetExpiryTime_ShouldBeInTheFuture()
    {
        var helper = CreateHelper();

        Assert.True(helper.GetExpiryTime() > DateTime.UtcNow);
    }
}
