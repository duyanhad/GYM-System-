using System.Linq;
using System.Reflection;
using GYM_Management_System.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace GYM_Management_System.UnitTests;

public class PermissionPolicyFactoryTests
{
    [Fact]
    public void PermissionConstants_ShouldNotContainDuplicateCodes()
    {
        var codes = typeof(PermissionConstants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Where(v => v != PermissionConstants.PERMISSION_CLAIM_TYPE)
            .ToList();

        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void Create_ShouldPrefixPolicyName()
    {
        var policy = PermissionPolicyFactory.Create(PermissionConstants.MEMBER_VIEW);

        Assert.Equal("Permission:MEMBER_VIEW", policy);
    }

    [Fact]
    public void RegisterAll_ShouldRegisterPolicyForEveryPermissionCode()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options => PermissionPolicyFactory.RegisterAll(options));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.GetPolicy(PermissionPolicyFactory.Create(PermissionConstants.MEMBER_VIEW)));
        Assert.NotNull(options.GetPolicy(PermissionPolicyFactory.Create(PermissionConstants.PAYMENT_CREATE)));
        Assert.NotNull(options.GetPolicy(PermissionPolicyFactory.Create(PermissionConstants.ADMIN_ROLE_UPDATE)));
    }
}
