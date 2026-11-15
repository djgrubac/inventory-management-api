using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Models;
using Inventory_Management.Domain.Entities;

namespace Microsoft.Extensions.DependencyInjection.Users.Queries.GetSingle;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, MeDTO>
{
    private readonly IAppUserRepository _users;
    private readonly ICurrentUserAccess _access;

    public GetMeQueryHandler(IAppUserRepository users, ICurrentUserAccess access)
    {
        _users = users;
        _access = access;
    }

    public async Task<MeDTO> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        // Only Auth0 logins have a Users row.
        var userId = _access.UserId ?? throw new NotFoundException("me", nameof(AppUser));

        var user = await _users.Query()
            .Where(u => u.Id == userId)
            .Select(UserDTO.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(userId.ToString(), nameof(AppUser));

        return new MeDTO
        {
            Uid = user.Uid,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            AssignableRoles = UserAccessRules.AssignableRoles(_access),
            Warehouses = user.Warehouses.OrderBy(w => w.WarehouseName).ToList()
        };
    }
}
