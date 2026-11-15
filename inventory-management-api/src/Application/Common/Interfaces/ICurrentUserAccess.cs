using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Application.Common.Interfaces;

// What the caller of the current request may do, from the Users table (Auth0 logins)
// or Identity roles (template logins).
public interface ICurrentUserAccess
{
    // AppUser.Id of an Auth0 login; null for Identity logins and anonymous requests.
    int? UserId { get; }

    string? Role { get; }

    // Administrators see and change every warehouse, so callers skip the permission checks.
    bool IsAdministrator { get; }

    // Warehouse id -> permission. Empty for Administrators, who need none.
    IReadOnlyDictionary<int, WarehousePermission> WarehousePermissions { get; }
}
