using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Common.Interfaces;

public interface IAppUserRepository
{
    // Read-only, with role and warehouse permissions (and their warehouses) loaded.
    IQueryable<AppUser> Query();

    // Tracked, with role and warehouse permissions loaded, so changes can be saved.
    Task<AppUser?> GetByAuth0IdAsync(string auth0UserId, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByUidAsync(Guid uid, CancellationToken cancellationToken = default);

    Task<AppRole?> GetRoleAsync(string name, CancellationToken cancellationToken = default);

    // Inserts the user, or returns the row another request inserted first for the same Auth0 id.
    Task<AppUser> AddOrGetExistingAsync(AppUser user, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
