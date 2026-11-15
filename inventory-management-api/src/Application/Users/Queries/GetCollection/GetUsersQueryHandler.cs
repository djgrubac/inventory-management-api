using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Models;
using Inventory_Management.Application.Common.Security;

namespace Microsoft.Extensions.DependencyInjection.Users.Queries.GetCollection;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, GetUsersResult>
{
    private readonly IAppUserRepository _users;
    private readonly ICurrentUserAccess _access;

    public GetUsersQueryHandler(IAppUserRepository users, ICurrentUserAccess access)
    {
        _users = users;
        _access = access;
    }

    public async Task<GetUsersResult> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _users.Query();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // ToLower on both sides so it's case-insensitive on Postgres and SQLite alike.
            var search = request.Search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(search) ||
                                     (u.Name != null && u.Name.ToLower().Contains(search)));
        }

        if (request.NewOnly)
            query = query.Where(u => u.RoleId == null);

        // Newest first, so people who just signed up are at the top; Id keeps paging stable.
        query = query.OrderByDescending(u => u.Created).ThenByDescending(u => u.Id);

        var users = await PaginatedList<UserDTO>
            .CreateAsync(query.Select(UserDTO.Projection), request.PageNumber, request.PageSize, cancellationToken);

        foreach (var user in users.Items)
        {
            // Warehouses the caller can't read stay hidden, as everywhere else.
            if (!_access.IsAdministrator)
                user.Warehouses = user.Warehouses.Where(w => _access.CanRead(w.WarehouseId)).ToList();

            user.CanManage = UserAccessRules.CanManage(_access, user.Id, user.Role);
        }

        return new GetUsersResult
        {
            Users = users.Items.ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = users.TotalCount,
            TotalPages = users.TotalPages
        };
    }
}
