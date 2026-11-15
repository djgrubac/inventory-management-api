using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Domain.Constants;

namespace Microsoft.Extensions.DependencyInjection.Users;

// Who may change whose access. Used by the list (to show Edit) and by the update (to enforce it).
public static class UserAccessRules
{
    // Roles the caller may give. Managers only bring in Staff.
    public static IReadOnlyList<string> AssignableRoles(ICurrentUserAccess access) =>
        access.IsAdministrator ? Roles.All
        : access.Role == Roles.Manager ? new[] { Roles.Staff }
        : Array.Empty<string>();

    public static bool CanManage(ICurrentUserAccess access, int targetUserId, string? targetRole)
    {
        // Nobody changes their own access, so an Administrator can't lock themselves out.
        if (access.UserId == targetUserId)
            return false;

        if (access.IsAdministrator)
            return true;

        // Managers handle new users and Staff, not other Managers or Administrators.
        return access.Role == Roles.Manager && (targetRole is null || targetRole == Roles.Staff);
    }
}
