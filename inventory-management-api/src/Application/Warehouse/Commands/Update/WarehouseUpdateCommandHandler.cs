using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Security;
using InventoryManagement.Core.Interfaces;
using Entities = Inventory_Management.Domain.Entities;

namespace Microsoft.Extensions.DependencyInjection.Warehouse.Commands.Update;

public class WarehouseUpdateCommandHandler: IRequestHandler<WarehouseUpdateCommand>
{
    private readonly IBaseRepository<Entities.Warehouse> _warehouseRepository;
    private readonly ICurrentUserAccess _access;

    public WarehouseUpdateCommandHandler(IBaseRepository<Entities.Warehouse> warehouseRepository, ICurrentUserAccess access)
    {
        _warehouseRepository = warehouseRepository;
        _access = access;
    }

    public async Task Handle(WarehouseUpdateCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseRepository.GetByUidAsync(request.Uid);
        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with Uid {request.Uid} not found.");
        }
        _access.EnsureCanWrite(warehouse.Id, request.Uid.ToString(), "Warehouse");
        
        warehouse.Name = request.Name;
        warehouse.Address = request.Address;
        warehouse.ContactPerson = request.ContactPerson;
        warehouse.Company = request.Company;
        warehouse.LastModified = DateTimeOffset.UtcNow;
        
        await _warehouseRepository.UpdateAsync(warehouse);
    }
}
