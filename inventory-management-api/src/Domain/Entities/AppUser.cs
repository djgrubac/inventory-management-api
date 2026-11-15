namespace Inventory_Management.Domain.Entities;

// An Auth0 account that has opened the app at least once. Named AppUser so it doesn't
// clash with Identity's ApplicationUser.
public class AppUser : BaseAuditableEntity
{
    // The Auth0 "sub" claim, e.g. "auth0|6720ab...".
    public string Auth0UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }

    // Null until an Administrator or Manager gives the user a role.
    public int? RoleId { get; set; }
    public AppRole? Role { get; set; }

    public List<UserWarehousePermission> WarehousePermissions { get; set; } = new();
}
