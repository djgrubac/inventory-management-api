namespace Inventory_Management.Domain.Entities;

public class UserWarehousePermission : BaseAuditableEntity
{
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public WarehousePermission Permission { get; set; }
}
