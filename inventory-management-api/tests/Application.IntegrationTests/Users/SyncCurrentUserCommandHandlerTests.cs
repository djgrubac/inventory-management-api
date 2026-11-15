using Inventory_Management.Domain.Constants;
using Inventory_Management.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection.Users;
using Microsoft.Extensions.DependencyInjection.Users.Commands.SyncCurrentUser;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Inventory_Management.Application.IntegrationTests.Users;

public class SyncCurrentUserCommandHandlerTests : IntegrationTestBase
{
    [Test]
    public async Task NewSub_CreatesUserWithoutRole()
    {
        var synced = await Sync(new SyncCurrentUserCommand("auth0|new", "new@yopmail.com", "Nina"));

        synced.Role.Should().BeNull();
        synced.WarehousePermissions.Should().BeEmpty();
        var user = await CreateContext().AppUsers.SingleAsync();
        user.Id.Should().Be(synced.Id);
        user.Auth0UserId.Should().Be("auth0|new");
        user.Email.Should().Be("new@yopmail.com");
        user.Name.Should().Be("Nina");
        user.RoleId.Should().BeNull();
    }

    [Test]
    public async Task ExistingUser_ChangedEmailAndName_AreUpdated_MissingClaimsKeepOldValues()
    {
        await Sync(new SyncCurrentUserCommand("auth0|u", "old@yopmail.com", "Old"));

        await Sync(new SyncCurrentUserCommand("auth0|u", "new@yopmail.com", "New"));
        await Sync(new SyncCurrentUserCommand("auth0|u", null, null));

        var user = await CreateContext().AppUsers.SingleAsync();
        user.Email.Should().Be("new@yopmail.com");
        user.Name.Should().Be("New");
    }

    [Test]
    public async Task InitialAdministrator_WithoutRole_BecomesAdministrator()
    {
        var synced = await Sync(new SyncCurrentUserCommand("auth0|boss", "boss@yopmail.com", null), "auth0|boss");

        synced.Role.Should().Be(Roles.Administrator);
        (await CreateContext().AppUsers.Include(u => u.Role).SingleAsync()).Role!.Name.Should().Be(Roles.Administrator);
    }

    [Test]
    public async Task InitialAdministrator_WithAnotherRole_KeepsIt()
    {
        await Sync(new SyncCurrentUserCommand("auth0|boss", "boss@yopmail.com", null));
        await using (var context = CreateContext())
        {
            var user = await context.AppUsers.SingleAsync();
            user.RoleId = (await context.AppRoles.SingleAsync(r => r.Name == Roles.Staff)).Id;
            await context.SaveChangesAsync();
        }

        var synced = await Sync(new SyncCurrentUserCommand("auth0|boss", "boss@yopmail.com", null), "auth0|boss");

        synced.Role.Should().Be(Roles.Staff);
    }

    [Test]
    public async Task OtherSub_IsNotMadeAdministrator()
    {
        var synced = await Sync(new SyncCurrentUserCommand("auth0|someone", "boss@yopmail.com", null), "auth0|boss");

        synced.Role.Should().BeNull();
    }

    [Test]
    public async Task ReturnsRoleAndWarehousePermissionsFromDatabase()
    {
        var north = TestData.Warehouse("North");
        var south = TestData.Warehouse("South");
        await SeedAsync(north, south);
        await using (var context = CreateContext())
        {
            context.AppUsers.Add(new AppUser
            {
                Auth0UserId = "auth0|staff",
                Email = "staff@yopmail.com",
                RoleId = (await context.AppRoles.SingleAsync(r => r.Name == Roles.Staff)).Id,
                WarehousePermissions =
                {
                    new UserWarehousePermission { WarehouseId = north.Id, Permission = WarehousePermission.Read },
                    new UserWarehousePermission { WarehouseId = south.Id, Permission = WarehousePermission.Write }
                }
            });
            await context.SaveChangesAsync();
        }

        var synced = await Sync(new SyncCurrentUserCommand("auth0|staff", "staff@yopmail.com", null));

        synced.Role.Should().Be(Roles.Staff);
        synced.WarehousePermissions.Should().BeEquivalentTo(new Dictionary<int, WarehousePermission>
        {
            [north.Id] = WarehousePermission.Read,
            [south.Id] = WarehousePermission.Write
        });
    }

    [Test]
    public async Task Repository_AddOrGetExisting_SameSubInsertedMeanwhile_ReturnsThatRow()
    {
        var first = await Sync(new SyncCurrentUserCommand("auth0|race", "race@yopmail.com", null));

        // A second request that didn't see the first row before inserting.
        var existing = await new AppUserRepository(CreateContext())
            .AddOrGetExistingAsync(new AppUser { Auth0UserId = "auth0|race", Email = "race@yopmail.com" });

        existing.Id.Should().Be(first.Id);
        (await CreateContext().AppUsers.CountAsync()).Should().Be(1);
    }

    private async Task<SyncedUser> Sync(SyncCurrentUserCommand command, params string[] initialAdministrators)
    {
        var handler = new SyncCurrentUserCommandHandler(
            new AppUserRepository(CreateContext()),
            Options.Create(new UsersOptions { InitialAdministrators = initialAdministrators.ToList() }),
            NullLogger<SyncCurrentUserCommandHandler>.Instance);
        return await handler.Handle(command, CancellationToken.None);
    }
}
