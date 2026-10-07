using Microsoft.AspNetCore.Authorization;

namespace GYM_Management_System.Authorization;

/// <summary>
/// Attribute dùng để gắn lên Controller / Action.
/// Tự sinh Policy tương ứng với PermissionCode.
/// Cú pháp: [HasPermission(PermissionConstants.MEMBER_VIEW)]
/// </summary>
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permissionCode)
        : base(policy: PermissionPolicyFactory.Create(permissionCode))
    {
    }
}
