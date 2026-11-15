using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Security;
using Inventory_Management.Domain.Entities;
using InventoryManagement.Core.Interfaces;
using Entities = Inventory_Management.Domain.Entities;

namespace Microsoft.Extensions.DependencyInjection.WarehouseStock.Commands;

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand>
{
    private readonly IStockRepository _stockRepository;
    private readonly IBaseRepository<Entities.Warehouse> _warehouseRepository;
    private readonly IBaseRepository<Entities.Product> _productRepository;
    private readonly ICurrentUserAccess _access;

    public AdjustStockCommandHandler
    (
        IStockRepository stockRepository, 
        IBaseRepository<Entities.Warehouse> warehouseRepository, 
        IBaseRepository<Entities.Product> productRepository,
        ICurrentUserAccess access
    )
    {
        _stockRepository = stockRepository;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
        _access = access;
    }

    public async Task Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetAsync(request.WarehouseUid, request.ProductUid);

        if (stock == null)
        {
            var warehouse = await _warehouseRepository.GetByUidAsync(request.WarehouseUid)
                            ?? throw new NotFoundException("Warehouse", request.WarehouseUid.ToString());
            _access.EnsureCanWrite(warehouse.Id, request.WarehouseUid.ToString(), "Warehouse");

            var product = await _productRepository.GetByUidAsync(request.ProductUid)
                          ?? throw new NotFoundException("Product", request.ProductUid.ToString());

            stock = new Stock
            {
                Warehouse = warehouse, 
                Product = product,      
                Quantity = request.NewQuantity
            };

            await _stockRepository.AddAsync(stock);
        }
        else
        {
            _access.EnsureCanWrite(stock.WarehouseId, request.WarehouseUid.ToString(), "Warehouse");
            stock.Quantity = request.NewQuantity;
            await _stockRepository.UpdateAsync(stock);
        }
    }
}
