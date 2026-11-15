namespace Inventory_Management.Domain.Enums;

public enum WarehousePermission
{
    // View the warehouse, its stock and its orders.
    Read = 1,
    // Also adjust stock and create or change orders.
    Write = 2
}
