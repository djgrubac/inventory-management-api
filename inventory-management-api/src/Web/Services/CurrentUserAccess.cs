using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Domain.Constants;
using Inventory_Management.Domain.Enums;
using Microsoft.Extensions.DependencyInjection.Users.Commands.SyncCurrentUser;

namespace Inventory_Management.Web.Services;

// The caller's role and warehouses for this request. Auth0 logins were already looked up
// in the Users table when the token was validated; template (Identity) logins only have
// their Identity roles and no warehouses.
public class CurrentUserAccess : ICurrentUserAccess
{
    private static readonly IReadOnlyDictionary<int, WarehousePermission> NoWarehouses =
        new Dictionary<int, WarehousePermission>();

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccess(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

    private SyncedUser? SyncedUser => HttpContext?.Items[typeof(SyncedUser)] as SyncedUser;

    public int? UserId => SyncedUser?.Id;

    public string? Role => SyncedUser is not null
        ? SyncedUser.Role
        : Roles.All.FirstOrDefault(role => HttpContext?.User.IsInRole(role) == true);

    public bool IsAdministrator => Role == Roles.Administrator;

    public IReadOnlyDictionary<int, WarehousePermission> WarehousePermissions =>
        SyncedUser?.WarehousePermissions ?? NoWarehouses;
}
