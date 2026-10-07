using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace GYM_Management_System.Authorization;

/// <summary>
/// Tạo tên Policy chuẩn hóa cho mỗi PermissionCode, đồng thời đăng ký
/// tất cả Policy vào AuthorizationOptions lúc startup.
/// Thêm Permission mới: chỉ cần thêm const trong PermissionConstants.
/// </summary>
public static class PermissionPolicyFactory
{
    public const string POLICY_PREFIX = "Permission:";

    public static string Create(string permissionCode)
        => POLICY_PREFIX + permissionCode;

    /// <summary>
    /// Duyệt tất cả hằng số string public trong PermissionConstants,
    /// đăng ký Policy tương ứng + gắn PermissionAuthorizationHandler.
    /// </summary>
    public static void RegisterAll(AuthorizationOptions options)
    {
        foreach (var code in GetAllPermissionCodes())
        {
            options.AddPolicy(Create(code), policy =>
            {
                policy.AddRequirements(new PermissionRequirement(code));
            });
        }
    }

    private static IEnumerable<string> GetAllPermissionCodes()
    {
        var fields = typeof(PermissionConstants)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        foreach (var f in fields)
        {
            if (f.IsLiteral && f.FieldType == typeof(string))
            {
                var value = (string)f.GetRawConstantValue()!;

                // Bỏ qua claim type (không phải permission code)
                if (value == PermissionConstants.PERMISSION_CLAIM_TYPE) continue;

                yield return value;
            }
        }
    }
}
