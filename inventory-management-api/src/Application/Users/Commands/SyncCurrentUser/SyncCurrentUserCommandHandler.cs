using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Domain.Constants;
using Inventory_Management.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection.Users.Commands.SyncCurrentUser;

public class SyncCurrentUserCommandHandler : IRequestHandler<SyncCurrentUserCommand, SyncedUser>
{
    private readonly IAppUserRepository _users;
    private readonly UsersOptions _options;
    private readonly ILogger<SyncCurrentUserCommandHandler> _logger;

    public SyncCurrentUserCommandHandler(
        IAppUserRepository users, IOptions<UsersOptions> options, ILogger<SyncCurrentUserCommandHandler> logger)
    {
        _users = users;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SyncedUser> Handle(SyncCurrentUserCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Auth0UserId);

        var user = await _users.GetByAuth0IdAsync(request.Auth0UserId, cancellationToken);
        if (user is null)
        {
            // First login: a row with no role, which the Users page lists as new.
            user = await _users.AddOrGetExistingAsync(new AppUser
            {
                Auth0UserId = request.Auth0UserId,
                Email = request.Email ?? string.Empty,
                Name = request.Name
            }, cancellationToken);
            // The id is what Users:InitialAdministrators needs.
            _logger.LogInformation("New user {Email} ({Auth0UserId}) signed up and is waiting for access.",
                user.Email, user.Auth0UserId);
        }

        // Most requests change nothing, so only write when something did.
        var changed = false;
        if (!string.IsNullOrWhiteSpace(request.Email) && user.Email != request.Email)
        {
            user.Email = request.Email;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(request.Name) && user.Name != request.Name)
        {
            user.Name = request.Name;
            changed = true;
        }
        if (user.RoleId is null && _options.InitialAdministrators.Contains(request.Auth0UserId))
        {
            user.Role = await _users.GetRoleAsync(Roles.Administrator, cancellationToken)
                ?? throw new InvalidOperationException($"Role '{Roles.Administrator}' is missing from the Roles table.");
            user.RoleId = user.Role.Id;
            changed = true;
        }
        if (changed)
        {
            await _users.SaveChangesAsync(cancellationToken);
        }

        return new SyncedUser(
            user.Id,
            user.Role?.Name,
            user.WarehousePermissions.ToDictionary(p => p.WarehouseId, p => p.Permission));
    }
}
