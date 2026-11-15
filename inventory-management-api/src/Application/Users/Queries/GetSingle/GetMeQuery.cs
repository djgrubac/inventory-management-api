using Inventory_Management.Application.Common.Models;

namespace Microsoft.Extensions.DependencyInjection.Users.Queries.GetSingle;

// The signed-in user: whether they have access yet, and to which warehouses.
public record GetMeQuery : IRequest<MeDTO>;

public class MeDTO
{
    public required Guid Uid { get; init; }
    public required string Email { get; init; }
    public string? Name { get; init; }

    // Null: waiting for access.
    public string? Role { get; init; }

    // Roles this user may give on the Users page; empty if they can't use it.
    public required IReadOnlyList<string> AssignableRoles { get; init; }

    // Empty for Administrators, who have every warehouse.
    public required List<UserWarehousePermissionDTO> Warehouses { get; init; }
}
