using Inventory_Management.Application.Common.Models;
using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Security;
using InventoryManagement.Core.Interfaces;
using Entities = Inventory_Management.Domain.Entities;

namespace Microsoft.Extensions.DependencyInjection.Warehouse.Queries;

public class GetWarehousesQueryHandler:IRequestHandler<GetWarehousesQuery, IEnumerable<WarehouseDTO>>
{
    private readonly IBaseRepository<Entities.Warehouse> _warehouseRepository;
    private readonly ICurrentUserAccess _access;

    public GetWarehousesQueryHandler(IBaseRepository<Entities.Warehouse> warehouseRepository, ICurrentUserAccess access)
    {
        _warehouseRepository = warehouseRepository;
        _access = access;
    }
    
    public async Task<IEnumerable<WarehouseDTO>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var warehouses = await _warehouseRepository.GetAllAsync();
        return warehouses.Where(w => _access.CanRead(w.Id)).Select(warehouses => new WarehouseDTO
        {
            Uid = warehouses.Uid,
            Company = warehouses.Company,
            Name = warehouses.Name,
            Address = warehouses.Address,
            ContactPerson = warehouses.ContactPerson
        });
    }
}
