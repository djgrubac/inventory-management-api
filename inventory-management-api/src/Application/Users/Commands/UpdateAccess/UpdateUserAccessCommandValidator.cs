using Inventory_Management.Domain.Constants;

namespace Microsoft.Extensions.DependencyInjection.Users.Commands.UpdateAccess;

public class UpdateUserAccessCommandValidator : AbstractValidator<UpdateUserAccessCommand>
{
    public UpdateUserAccessCommandValidator()
    {
        RuleFor(x => x.UserUid)
            .NotEmpty();

        RuleFor(x => x.Role)
            .Must(role => role is null || Roles.All.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}.");

        RuleFor(x => x.Warehouses)
            .NotNull()
            .Must(warehouses => warehouses.Select(w => w.WarehouseUid).Distinct().Count() == warehouses.Count)
            .WithMessage("Each warehouse can only be listed once.");

        RuleForEach(x => x.Warehouses).ChildRules(warehouse =>
        {
            warehouse.RuleFor(w => w.WarehouseUid).NotEmpty();
            warehouse.RuleFor(w => w.Permission).IsInEnum();
        });
    }
}
