namespace Inventory_Management.Domain.Entities;

// One row per name in Constants.Roles. Controllers check roles by name, so a row added
// here does nothing until code uses it.
public class AppRole : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
