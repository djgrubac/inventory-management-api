using Inventory_Management.Domain.Enums;

namespace Microsoft.Extensions.DependencyInjection.Users.Commands.SyncCurrentUser;

// Sent by the API for every request with an Auth0 token. Email and name come from
// custom claims the Auth0 post-login Action adds; they're null if the Action doesn't.
public record SyncCurrentUserCommand(string Auth0UserId, string? Email, string? Name) : IRequest<SyncedUser>;

// What the rest of the request needs: the role and warehouse permissions from our database.
public record SyncedUser(int Id, string? Role, IReadOnlyDictionary<int, WarehousePermission> WarehousePermissions);
