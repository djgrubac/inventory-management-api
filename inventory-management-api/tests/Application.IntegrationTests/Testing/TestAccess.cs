using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Domain.Constants;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Application.IntegrationTests.Testing;

// A fixed caller for handlers that check warehouse permissions.
public class TestAccess : ICurrentUserAccess
{
    public int? UserId { get; init; }
    public string? Role { get; init; }
    public bool IsAdministrator { get; init; }
    public IReadOnlyDictionary<int, WarehousePermission> WarehousePermissions { get; init; } =
        new Dictionary<int, WarehousePermission>();

    public static TestAccess Administrator(int userId = 1) =>
        new() { UserId = userId, Role = Roles.Administrator, IsAdministrator = true };

    public static TestAccess Manager(int userId, params (int WarehouseId, WarehousePermission Permission)[] warehouses) =>
        new() { UserId = userId, Role = Roles.Manager, WarehousePermissions = warehouses.ToDictionary(w => w.WarehouseId, w => w.Permission) };

    public static TestAccess Staff(int userId, params (int WarehouseId, WarehousePermission Permission)[] warehouses) =>
        new() { UserId = userId, Role = Roles.Staff, WarehousePermissions = warehouses.ToDictionary(w => w.WarehouseId, w => w.Permission) };
}
