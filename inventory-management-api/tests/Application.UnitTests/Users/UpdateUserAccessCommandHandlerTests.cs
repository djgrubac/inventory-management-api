using Ardalis.GuardClauses;
using Inventory_Management.Application.Common.Exceptions;
using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Domain.Constants;
using Inventory_Management.Domain.Enums;
using InventoryManagement.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection.Users;
using Microsoft.Extensions.DependencyInjection.Users.Commands.UpdateAccess;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Entities = Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.UnitTests.Users;

public class UpdateUserAccessCommandHandlerTests
{
    private const int AdminId = 1, ManagerId = 2, TargetId = 10;

    private readonly Entities.AppRole _administrator = new() { Id = 1, Name = Roles.Administrator };
    private readonly Entities.AppRole _manager = new() { Id = 2, Name = Roles.Manager };
    private readonly Entities.AppRole _staff = new() { Id = 3, Name = Roles.Staff };

    private Entities.Warehouse _north = null!, _south = null!, _east = null!;
    private Entities.AppUser _target = null!;
    private Mock<IAppUserRepository> _users = null!;
    private Mock<IBaseRepository<Entities.Warehouse>> _warehouses = null!;
    private Mock<IEmailSender> _emailSender = null!;
    private (string To, string Subject, string Text)? _sentEmail;

    [SetUp]
    public void Setup()
    {
        _north = new Entities.Warehouse { Id = 1, Name = "North" };
        _south = new Entities.Warehouse { Id = 2, Name = "South" };
        _east = new Entities.Warehouse { Id = 3, Name = "East" };
        _target = new Entities.AppUser { Id = TargetId, Auth0UserId = "auth0|target", Email = "new@yopmail.com", Name = "Nina" };

        _users = new Mock<IAppUserRepository>();
        _users.Setup(r => r.GetByUidAsync(_target.Uid, It.IsAny<CancellationToken>())).ReturnsAsync(_target);
        foreach (var role in new[] { _administrator, _manager, _staff })
            _users.Setup(r => r.GetRoleAsync(role.Name, It.IsAny<CancellationToken>())).ReturnsAsync(role);

        _warehouses = new Mock<IBaseRepository<Entities.Warehouse>>();
        _warehouses.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { _north, _south, _east });

        _sentEmail = null;
        _emailSender = new Mock<IEmailSender>();
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string, CancellationToken>((to, subject, _, text, _) => _sentEmail = (to, subject, text))
            .ReturnsAsync(true);
    }

    [Test]
    public async Task Administrator_GivesStaffWithWarehouses_SavesAndEmailsRoleDutiesAndWarehouses()
    {
        var result = await Handle(TestAccess.Administrator(AdminId), Roles.Staff,
            (_north, WarehousePermission.Read), (_south, WarehousePermission.Write));

        result.Changed.Should().BeTrue();
        result.EmailSent.Should().BeTrue();
        _target.RoleId.Should().Be(_staff.Id);
        Permissions().Should().BeEquivalentTo(new[] { (1, WarehousePermission.Read), (2, WarehousePermission.Write) });
        _users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _sentEmail.Should().NotBeNull();
        _sentEmail!.Value.To.Should().Be("new@yopmail.com");
        _sentEmail.Value.Subject.Should().Contain(Roles.Staff);
        _sentEmail.Value.Text.Should().Contain("Hello Nina,")
            .And.Contain(RoleDuties.For(Roles.Staff))
            .And.Contain("North: read only")
            .And.Contain("South: read and write")
            .And.NotContain("East");
    }

    [Test]
    public async Task Administrator_LeavesWarehouseOut_RemovesItsPermission()
    {
        GiveTarget(_staff, (_north, WarehousePermission.Write), (_south, WarehousePermission.Read));

        await Handle(TestAccess.Administrator(AdminId), Roles.Staff, (_south, WarehousePermission.Read));

        Permissions().Should().Equal((2, WarehousePermission.Read));
    }

    [Test]
    public async Task Administrator_ChangesPermission_UpdatesTheRow()
    {
        GiveTarget(_staff, (_north, WarehousePermission.Read));
        var row = _target.WarehousePermissions.Single();

        await Handle(TestAccess.Administrator(AdminId), Roles.Staff, (_north, WarehousePermission.Write));

        _target.WarehousePermissions.Should().ContainSingle().Which.Should().BeSameAs(row);
        row.Permission.Should().Be(WarehousePermission.Write);
    }

    [Test]
    public async Task Administrator_RemovesRole_EmailsThatAccessWasRemoved()
    {
        GiveTarget(_staff);

        var result = await Handle(TestAccess.Administrator(AdminId), role: null);

        result.Changed.Should().BeTrue();
        _target.RoleId.Should().BeNull();
        _target.Role.Should().BeNull();
        _sentEmail!.Value.Subject.Should().Contain("removed");
    }

    [Test]
    public async Task ChangingOwnAccess_IsForbidden()
    {
        var act = () => Handle(TestAccess.Administrator(TargetId), Roles.Staff);

        (await act.Should().ThrowAsync<ForbiddenAccessException>()).WithMessage("You can't change your own access.");
        _users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task NothingChanged_DoesNotSaveOrEmail()
    {
        GiveTarget(_staff, (_north, WarehousePermission.Read));

        var result = await Handle(TestAccess.Administrator(AdminId), Roles.Staff, (_north, WarehousePermission.Read));

        result.Changed.Should().BeFalse();
        result.EmailSent.Should().BeFalse();
        _users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _sentEmail.Should().BeNull();
    }

    [Test]
    public async Task EmailFails_ChangeIsStillSavedAndEmailSentIsFalse()
    {
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var result = await Handle(TestAccess.Administrator(AdminId), Roles.Staff, (_north, WarehousePermission.Read));

        result.Changed.Should().BeTrue();
        result.EmailSent.Should().BeFalse();
        _users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task NoEmailServerConfigured_ChangeIsSavedAndEmailSentIsFalse()
    {
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await Handle(TestAccess.Administrator(AdminId), Roles.Staff, (_north, WarehousePermission.Read));

        result.Changed.Should().BeTrue();
        result.EmailSent.Should().BeFalse();
    }

    [Test]
    public async Task UnknownWarehouse_ThrowsNotFound()
    {
        var act = () => Handle(TestAccess.Administrator(AdminId), Roles.Staff,
            (new Entities.Warehouse { Id = 99, Name = "Ghost" }, WarehousePermission.Read));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Manager_GivesStaffOnHisWriteWarehouse_Saves()
    {
        var manager = TestAccess.Manager(ManagerId, (1, WarehousePermission.Write));

        var result = await Handle(manager, Roles.Staff, (_north, WarehousePermission.Write));

        result.Changed.Should().BeTrue();
        _target.RoleId.Should().Be(_staff.Id);
        Permissions().Should().Equal((1, WarehousePermission.Write));
    }

    [Test]
    public async Task Manager_GivesManagerRole_IsForbidden()
    {
        var act = () => Handle(TestAccess.Manager(ManagerId, (1, WarehousePermission.Write)), Roles.Manager);

        (await act.Should().ThrowAsync<ForbiddenAccessException>()).WithMessage("Managers can only give the Staff role.");
    }

    [Test]
    public async Task Manager_ChangesAnotherManager_IsForbidden()
    {
        GiveTarget(_manager);

        var act = () => Handle(TestAccess.Manager(ManagerId, (1, WarehousePermission.Write)), Roles.Staff);

        (await act.Should().ThrowAsync<ForbiddenAccessException>())
            .WithMessage("Managers can only change the access of new users and Staff.");
    }

    [Test]
    public async Task Manager_GrantsWarehouseHeOnlyReads_IsForbidden()
    {
        var manager = TestAccess.Manager(ManagerId, (1, WarehousePermission.Write), (2, WarehousePermission.Read));

        var act = () => Handle(manager, Roles.Staff, (_south, WarehousePermission.Read));

        (await act.Should().ThrowAsync<ForbiddenAccessException>())
            .WithMessage("You can only give access to warehouses where you have Write.");
        _users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Manager_GrantsWarehouseHeCantSee_ThrowsNotFound()
    {
        var act = () => Handle(TestAccess.Manager(ManagerId, (1, WarehousePermission.Write)), Roles.Staff,
            (_east, WarehousePermission.Read));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Manager_OnlyChangesHisWriteWarehouses_OtherRowsStay()
    {
        // South: the manager can only read it. East: he can't see it at all.
        GiveTarget(_staff, (_south, WarehousePermission.Write), (_east, WarehousePermission.Read));
        var manager = TestAccess.Manager(ManagerId, (1, WarehousePermission.Write), (2, WarehousePermission.Read));

        await Handle(manager, Roles.Staff, (_north, WarehousePermission.Read));

        Permissions().Should().BeEquivalentTo(new[]
        {
            (1, WarehousePermission.Read), (2, WarehousePermission.Write), (3, WarehousePermission.Read)
        });
    }

    [Test]
    public async Task Manager_RepeatsRowOnWarehouseHeOnlyReads_IsAllowed()
    {
        GiveTarget(_staff, (_south, WarehousePermission.Write));
        var manager = TestAccess.Manager(ManagerId, (1, WarehousePermission.Write), (2, WarehousePermission.Read));

        var result = await Handle(manager, Roles.Staff, (_north, WarehousePermission.Read), (_south, WarehousePermission.Write));

        result.Changed.Should().BeTrue();
        Permissions().Should().BeEquivalentTo(new[] { (1, WarehousePermission.Read), (2, WarehousePermission.Write) });
    }

    private Task<UpdateUserAccessResult> Handle(
        ICurrentUserAccess access, string? role, params (Entities.Warehouse Warehouse, WarehousePermission Permission)[] warehouses)
    {
        var handler = new UpdateUserAccessCommandHandler(
            _users.Object,
            _warehouses.Object,
            access,
            _emailSender.Object,
            Options.Create(new UsersOptions { WebBaseUrl = "http://localhost:5173" }),
            NullLogger<UpdateUserAccessCommandHandler>.Instance);

        return handler.Handle(new UpdateUserAccessCommand
        {
            UserUid = _target.Uid,
            Role = role,
            Warehouses = warehouses
                .Select(w => new UserWarehousePermissionItem { WarehouseUid = w.Warehouse.Uid, Permission = w.Permission })
                .ToList()
        }, CancellationToken.None);
    }

    private void GiveTarget(Entities.AppRole role, params (Entities.Warehouse Warehouse, WarehousePermission Permission)[] warehouses)
    {
        _target.Role = role;
        _target.RoleId = role.Id;
        _target.WarehousePermissions = warehouses
            .Select(w => new Entities.UserWarehousePermission
            {
                UserId = TargetId, WarehouseId = w.Warehouse.Id, Warehouse = w.Warehouse, Permission = w.Permission
            })
            .ToList();
    }

    private IEnumerable<(int, WarehousePermission)> Permissions() =>
        _target.WarehousePermissions.Select(p => (p.WarehouseId, p.Permission));
}
