using Inventory_Management.Application.Common.Exceptions;
using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Application.Common.Security;

public static class CurrentUserAccessExtensions
{
    public static bool CanRead(this ICurrentUserAccess access, int warehouseId) =>
        access.IsAdministrator || access.WarehousePermissions.ContainsKey(warehouseId);

    public static bool CanWrite(this ICurrentUserAccess access, int warehouseId) =>
        access.IsAdministrator ||
        (access.WarehousePermissions.TryGetValue(warehouseId, out var permission) && permission == WarehousePermission.Write);

    // Ids to filter queries by. Only meaningful when the caller isn't an Administrator.
    public static List<int> ReadableWarehouseIds(this ICurrentUserAccess access) =>
        access.WarehousePermissions.Keys.ToList();

    // A warehouse the caller can't read behaves as if it doesn't exist.
    public static void EnsureCanRead(this ICurrentUserAccess access, int warehouseId, string key, string objectName)
    {
        if (!access.CanRead(warehouseId))
            throw new NotFoundException(key, objectName);
    }

    // Readable but read-only: the caller can see it, so say why the change is refused.
    public static void EnsureCanWrite(this ICurrentUserAccess access, int warehouseId, string key, string objectName)
    {
        access.EnsureCanRead(warehouseId, key, objectName);
        if (!access.CanWrite(warehouseId))
            throw new ForbiddenAccessException("You have read-only access to this warehouse.");
    }
}
