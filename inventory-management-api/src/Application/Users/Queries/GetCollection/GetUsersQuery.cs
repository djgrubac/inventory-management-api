using Inventory_Management.Application.Common.Models;

namespace Microsoft.Extensions.DependencyInjection.Users.Queries.GetCollection;

public class GetUsersQuery : IRequest<GetUsersResult>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    // Part of the email or name.
    public string? Search { get; set; }

    // Only users waiting for access (no role yet).
    public bool NewOnly { get; set; }
}

public class GetUsersResult
{
    public List<UserDTO> Users { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}
