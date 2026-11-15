using Inventory_Management.Application.Common.Exceptions;
using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Security;
using Inventory_Management.Domain.Constants;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using InventoryManagement.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Entities = Inventory_Management.Domain.Entities;

namespace Microsoft.Extensions.DependencyInjection.Users.Commands.UpdateAccess;

public class UpdateUserAccessCommandHandler : IRequestHandler<UpdateUserAccessCommand, UpdateUserAccessResult>
{
    private readonly IAppUserRepository _users;
    private readonly IBaseRepository<Entities.Warehouse> _warehouses;
    private readonly ICurrentUserAccess _access;
    private readonly IEmailSender _emailSender;
    private readonly UsersOptions _options;
    private readonly ILogger<UpdateUserAccessCommandHandler> _logger;

    public UpdateUserAccessCommandHandler(
        IAppUserRepository users,
        IBaseRepository<Entities.Warehouse> warehouses,
        ICurrentUserAccess access,
        IEmailSender emailSender,
        IOptions<UsersOptions> options,
        ILogger<UpdateUserAccessCommandHandler> logger)
    {
        _users = users;
        _warehouses = warehouses;
        _access = access;
        _emailSender = emailSender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<UpdateUserAccessResult> Handle(UpdateUserAccessCommand request, CancellationToken cancellationToken)
    {
        var target = await _users.GetByUidAsync(request.UserUid, cancellationToken)
            ?? throw new NotFoundException(request.UserUid.ToString(), nameof(AppUser));

        EnsureCanManage(target);

        if (request.Role is not null && !UserAccessRules.AssignableRoles(_access).Contains(request.Role))
            throw new ForbiddenAccessException("Managers can only give the Staff role.");

        var warehousesById = (await _warehouses.GetAllAsync()).ToDictionary(w => w.Id);
        var desired = ResolveWarehouses(request.Warehouses, warehousesById);
        var current = target.WarehousePermissions.ToDictionary(p => p.WarehouseId);

        // On warehouses the caller can't change, the request may only repeat what's there.
        foreach (var (warehouseId, permission) in desired.Where(d => !_access.CanWrite(d.Key)))
        {
            if (!current.TryGetValue(warehouseId, out var row) || row.Permission != permission)
                throw new ForbiddenAccessException("You can only give access to warehouses where you have Write.");
        }

        var changed = false;

        if (target.Role?.Name != request.Role)
        {
            target.Role = request.Role is null
                ? null
                : await _users.GetRoleAsync(request.Role, cancellationToken)
                  ?? throw new InvalidOperationException($"Role '{request.Role}' is missing from the Roles table.");
            target.RoleId = target.Role?.Id;
            changed = true;
        }

        foreach (var warehouseId in warehousesById.Keys.Where(_access.CanWrite))
        {
            var has = current.TryGetValue(warehouseId, out var row);
            var wants = desired.TryGetValue(warehouseId, out var permission);

            if (has && !wants)
            {
                target.WarehousePermissions.Remove(row!);
                changed = true;
            }
            else if (!has && wants)
            {
                target.WarehousePermissions.Add(new UserWarehousePermission { WarehouseId = warehouseId, Permission = permission });
                changed = true;
            }
            else if (has && wants && row!.Permission != permission)
            {
                row.Permission = permission;
                changed = true;
            }
        }

        if (!changed)
            return new UpdateUserAccessResult { Changed = false, EmailSent = false };

        await _users.SaveChangesAsync(cancellationToken);

        return new UpdateUserAccessResult
        {
            Changed = true,
            EmailSent = await TrySendEmailAsync(target, warehousesById, cancellationToken)
        };
    }

    private void EnsureCanManage(AppUser target)
    {
        if (UserAccessRules.CanManage(_access, target.Id, target.Role?.Name))
            return;

        if (_access.UserId == target.Id)
            throw new ForbiddenAccessException("You can't change your own access.");
        if (_access.Role == Roles.Manager)
            throw new ForbiddenAccessException("Managers can only change the access of new users and Staff.");
        throw new ForbiddenAccessException("Only Administrators and Managers can change access.");
    }

    // Warehouse uid -> id. One the caller can't read behaves as if it doesn't exist.
    private Dictionary<int, WarehousePermission> ResolveWarehouses(
        List<UserWarehousePermissionItem> items, Dictionary<int, Entities.Warehouse> warehousesById)
    {
        var idsByUid = warehousesById.Values.ToDictionary(w => w.Uid, w => w.Id);
        var desired = new Dictionary<int, WarehousePermission>();
        foreach (var item in items)
        {
            if (!idsByUid.TryGetValue(item.WarehouseUid, out var warehouseId) || !_access.CanRead(warehouseId))
                throw new NotFoundException(item.WarehouseUid.ToString(), nameof(Entities.Warehouse));
            desired[warehouseId] = item.Permission;
        }
        return desired;
    }

    // A failed email never undoes the saved change; the caller is told so they can follow up.
    private async Task<bool> TrySendEmailAsync(AppUser target, Dictionary<int, Entities.Warehouse> warehousesById, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(target.Email))
        {
            _logger.LogWarning("User {UserUid} has no email address, so no access email was sent.", target.Uid);
            return false;
        }

        var email = UserAccessEmail.Build(
            target.Name,
            target.Role?.Name,
            target.WarehousePermissions.Select(p => (warehousesById[p.WarehouseId].Name ?? string.Empty, p.Permission)),
            _options.WebBaseUrl);

        try
        {
            return await _emailSender.SendAsync(target.Email, email.Subject, email.Html, email.Text, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending the access email to user {UserUid} failed.", target.Uid);
            return false;
        }
    }
}
