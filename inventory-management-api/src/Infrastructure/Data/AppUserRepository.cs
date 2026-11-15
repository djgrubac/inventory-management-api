using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Infrastructure.Data;

public class AppUserRepository : IAppUserRepository
{
    private readonly ApplicationDbContext _context;

    public AppUserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<AppUser> Query()
    {
        return _context.AppUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.WarehousePermissions)
                .ThenInclude(p => p.Warehouse);
    }

    public Task<AppUser?> GetByAuth0IdAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        return WithAccess().FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId, cancellationToken);
    }

    public Task<AppUser?> GetByUidAsync(Guid uid, CancellationToken cancellationToken = default)
    {
        return WithAccess().FirstOrDefaultAsync(u => u.Uid == uid, cancellationToken);
    }

    public Task<AppRole?> GetRoleAsync(string name, CancellationToken cancellationToken = default)
    {
        return _context.AppRoles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<AppUser> AddOrGetExistingAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        _context.AppUsers.Add(user);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException)
        {
            // A parallel first request (the web app loads several things at once) inserted
            // the same Auth0 id between our lookup and this insert; use its row instead.
            _context.Entry(user).State = EntityState.Detached;
            var existing = await GetByAuth0IdAsync(user.Auth0UserId, cancellationToken);
            if (existing is null) throw;
            return existing;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AppUser> WithAccess()
    {
        return _context.AppUsers
            .Include(u => u.Role)
            .Include(u => u.WarehousePermissions)
                .ThenInclude(p => p.Warehouse);
    }
}
