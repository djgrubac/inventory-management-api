using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Domain.Constants;
using Inventory_Management.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection.Users.Queries.GetCollection;

namespace Inventory_Management.Application.IntegrationTests.Users;

public class GetUsersQueryHandlerTests : IntegrationTestBase
{
    private Warehouse _north = null!, _south = null!;
    private AppUser _admin = null!, _manager = null!, _staff = null!, _newUser = null!;

    // Four users, one hour apart: _admin oldest, _newUser newest.
    [SetUp]
    public async Task SeedUsers()
    {
        _north = TestData.Warehouse("North");
        _south = TestData.Warehouse("South");
        await SeedAsync(_north, _south);

        await using var context = CreateContext();
        var roles = await context.AppRoles.ToDictionaryAsync(r => r.Name, r => r.Id);
        _admin = User("auth0|a", "admin@yopmail.com", "Ada", roles[Roles.Administrator], 1);
        _manager = User("auth0|m", "manager@yopmail.com", "Max", roles[Roles.Manager], 2,
            (_north, WarehousePermission.Write));
        _staff = User("auth0|s", "staff@yopmail.com", "Sam", roles[Roles.Staff], 3,
            (_north, WarehousePermission.Read), (_south, WarehousePermission.Write));
        _newUser = User("auth0|n", "Nina.New@yopmail.com", null, null, 4);
        context.AppUsers.AddRange(_admin, _manager, _staff, _newUser);
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task NoFilters_ReturnsEveryoneNewestFirst_WithRolesAndWarehouses()
    {
        var result = await Handle(TestAccess.Administrator(_admin.Id), new GetUsersQuery());

        result.Users.Select(u => u.Email).Should().Equal(
            "Nina.New@yopmail.com", "staff@yopmail.com", "manager@yopmail.com", "admin@yopmail.com");
        result.TotalCount.Should().Be(4);
        result.Users[0].Role.Should().BeNull();
        result.Users[1].Role.Should().Be(Roles.Staff);
        result.Users[1].Warehouses.Select(w => (w.WarehouseName, w.Permission)).Should().BeEquivalentTo(new[]
        {
            ("North", WarehousePermission.Read), ("South", WarehousePermission.Write)
        });
    }

    [Test]
    public async Task Search_MatchesEmailOrNameIgnoringCase()
    {
        (await Emails(new GetUsersQuery { Search = "nina.NEW" })).Should().Equal("Nina.New@yopmail.com");
        (await Emails(new GetUsersQuery { Search = "max" })).Should().Equal("manager@yopmail.com");
    }

    [Test]
    public async Task NewOnly_ReturnsUsersWithoutRole()
    {
        (await Emails(new GetUsersQuery { NewOnly = true })).Should().Equal("Nina.New@yopmail.com");
    }

    [Test]
    public async Task Paging_ReturnsRequestedPage()
    {
        var result = await Handle(TestAccess.Administrator(_admin.Id), new GetUsersQuery { PageNumber = 2, PageSize = 3 });

        result.Users.Select(u => u.Email).Should().Equal("admin@yopmail.com");
        result.TotalPages.Should().Be(2);
        result.TotalCount.Should().Be(4);
    }

    [Test]
    public async Task Administrator_CanManageEveryoneButHimself()
    {
        var result = await Handle(TestAccess.Administrator(_admin.Id), new GetUsersQuery());

        result.Users.ToDictionary(u => u.Email, u => u.CanManage).Should().BeEquivalentTo(new Dictionary<string, bool>
        {
            ["Nina.New@yopmail.com"] = true, ["staff@yopmail.com"] = true,
            ["manager@yopmail.com"] = true, ["admin@yopmail.com"] = false
        });
    }

    [Test]
    public async Task Manager_CanManageNewAndStaff_AndOnlySeesHisWarehouses()
    {
        var manager = TestAccess.Manager(_manager.Id, (_north.Id, WarehousePermission.Write));

        var result = await Handle(manager, new GetUsersQuery());

        result.Users.ToDictionary(u => u.Email, u => u.CanManage).Should().BeEquivalentTo(new Dictionary<string, bool>
        {
            ["Nina.New@yopmail.com"] = true, ["staff@yopmail.com"] = true,
            ["manager@yopmail.com"] = false, ["admin@yopmail.com"] = false
        });
        result.Users.Single(u => u.Email == "staff@yopmail.com").Warehouses
            .Select(w => w.WarehouseName).Should().Equal("North");
    }

    private async Task<GetUsersResult> Handle(ICurrentUserAccess access, GetUsersQuery query) =>
        await new GetUsersQueryHandler(new AppUserRepository(CreateContext()), access).Handle(query, CancellationToken.None);

    private async Task<IEnumerable<string>> Emails(GetUsersQuery query) =>
        (await Handle(TestAccess.Administrator(_admin.Id), query)).Users.Select(u => u.Email);

    private static AppUser User(string sub, string email, string? name, int? roleId, int hour,
        params (Warehouse Warehouse, WarehousePermission Permission)[] warehouses) => new()
    {
        Auth0UserId = sub,
        Email = email,
        Name = name,
        RoleId = roleId,
        Created = TestData.BaseTime.AddHours(hour),
        WarehousePermissions = warehouses
            .Select(w => new UserWarehousePermission { WarehouseId = w.Warehouse.Id, Permission = w.Permission })
            .ToList()
    };
}
