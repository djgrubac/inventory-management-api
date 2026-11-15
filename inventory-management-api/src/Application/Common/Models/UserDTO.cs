using System.Linq.Expressions;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Application.Common.Models;

public class UserDTO
{
    // Internal id, used by the handlers; not sent.
    [System.Text.Json.Serialization.JsonIgnore]
    public int Id { get; init; }
    public required Guid Uid { get; init; }
    public required string Email { get; init; }
    public string? Name { get; init; }

    // Null: the user signed up and is waiting for access.
    public string? Role { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required List<UserWarehousePermissionDTO> Warehouses { get; set; }

    // Whether the caller may change this user's access (set by the handler).
    public bool CanManage { get; set; }

    public static Expression<Func<AppUser, UserDTO>> Projection
    {
        get
        {
            return entity => new UserDTO
            {
                Id = entity.Id,
                Uid = entity.Uid,
                Email = entity.Email,
                Name = entity.Name,
                Role = entity.Role != null ? entity.Role.Name : null,
                Created = entity.Created,
                Warehouses = entity.WarehousePermissions
                    .Select(p => new UserWarehousePermissionDTO
                    {
                        WarehouseId = p.WarehouseId,
                        WarehouseUid = p.Warehouse.Uid,
                        WarehouseName = p.Warehouse.Name ?? string.Empty,
                        Permission = p.Permission
                    })
                    .ToList()
            };
        }
    }
}

public class UserWarehousePermissionDTO
{
    // Internal id, used by the handlers to filter by the caller's warehouses; not sent.
    [System.Text.Json.Serialization.JsonIgnore]
    public int WarehouseId { get; init; }
    public required Guid WarehouseUid { get; init; }
    public required string WarehouseName { get; init; }
    public required WarehousePermission Permission { get; init; }
}
