using Inventory_Management.Application.Common.Models;
using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Security;
using InventoryManagement.Core.Interfaces;
using Entities = Inventory_Management.Domain.Entities;

namespace Microsoft.Extensions.DependencyInjection.Warehouse.Queries.GetSingle;

public class GetWarehouseByIdQueryHandler:IRequestHandler<GetWarehouseByIdQuery,WarehouseDTO?>
{
    private readonly IBaseRepository<Entities.Warehouse> _warehouseRepository;
    private readonly ICurrentUserAccess _access;

    public GetWarehouseByIdQueryHandler(IBaseRepository<Entities.Warehouse> warehouseRepository, ICurrentUserAccess access)
    {
        _warehouseRepository = warehouseRepository;
        _access = access;
    }
    
    public async Task<WarehouseDTO?> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseRepository.GetByUidAsync(request.Uid);
        if (warehouse == null || !_access.CanRead(warehouse.Id))
            return null;
        
        return new WarehouseDTO
        {
            Uid = warehouse.Uid,
            Company = warehouse.Company,
            Name = warehouse.Name,
            Address = warehouse.Address,
            ContactPerson = warehouse.ContactPerson,
        };
    }
}
