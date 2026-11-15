using Inventory_Management.Domain.Enums;

namespace Microsoft.Extensions.DependencyInjection.Users.Commands.UpdateAccess;

// Sets a user's role and warehouse permissions, then emails them what they can do now.
public class UpdateUserAccessCommand : IRequest<UpdateUserAccessResult>
{
    // From the route.
    public Guid UserUid { get; set; }

    // Null removes the role: the user is back to "waiting for access".
    public string? Role { get; set; }

    // Every warehouse the user should have, as far as the caller can see. A warehouse left
    // out loses its permission if the caller has Write on it; others are left as they are.
    public List<UserWarehousePermissionItem> Warehouses { get; set; } = new();
}

public class UserWarehousePermissionItem
{
    public Guid WarehouseUid { get; set; }
    public WarehousePermission Permission { get; set; }
}

public class UpdateUserAccessResult
{
    // False when the request matched what was already saved; nothing is saved or emailed then.
    public bool Changed { get; init; }

    // False if sending failed or no email server is configured; the change is saved either way.
    public bool EmailSent { get; init; }
}
