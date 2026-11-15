using Inventory_Management.Domain.Enums;
using Microsoft.Extensions.DependencyInjection.Users.Commands.UpdateAccess;

namespace Inventory_Management.Application.UnitTests.Users;

public class UpdateUserAccessCommandValidatorTests
{
    private readonly UpdateUserAccessCommandValidator _validator = new();

    [Test]
    public void ValidCommand_Passes()
    {
        var command = Command("Staff", Guid.NewGuid(), Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Test]
    public void NoRole_Passes()
    {
        _validator.Validate(Command(null)).IsValid.Should().BeTrue();
    }

    [Test]
    public void UnknownRole_Fails()
    {
        var result = _validator.Validate(Command("Owner"));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateUserAccessCommand.Role));
    }

    [Test]
    public void SameWarehouseTwice_Fails()
    {
        var warehouse = Guid.NewGuid();

        var result = _validator.Validate(Command("Staff", warehouse, warehouse));

        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Each warehouse can only be listed once.");
    }

    [Test]
    public void UnknownPermission_Fails()
    {
        var command = Command("Staff", Guid.NewGuid());
        command.Warehouses[0].Permission = (WarehousePermission)7;

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    private static UpdateUserAccessCommand Command(string? role, params Guid[] warehouses) => new()
    {
        UserUid = Guid.NewGuid(),
        Role = role,
        Warehouses = warehouses
            .Select(uid => new UserWarehousePermissionItem { WarehouseUid = uid, Permission = WarehousePermission.Read })
            .ToList()
    };
}
