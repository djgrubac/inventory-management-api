using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Models;
using Inventory_Management.Application.Common.Security;
using Inventory_Management.Domain.Entities;

namespace Microsoft.Extensions.DependencyInjection.WarehouseStock.Queries;

public class GetStockByUidQueryHandler : IRequestHandler<GetStockByUidQuery, StockDTO?>
{
    private readonly IStockRepository _stockRepository;
    private readonly ICurrentUserAccess _access;

    public GetStockByUidQueryHandler(IStockRepository stockRepository, ICurrentUserAccess access)
    {
        _stockRepository = stockRepository;
        _access = access;
    }
    
    public async Task<StockDTO?> Handle(GetStockByUidQuery request, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetAsync(request.WarehouseUid, request.ProductUid);
        
        if (stock == null || !_access.CanRead(stock.WarehouseId))
            return null;

        return StockDTO.Projection.Compile().Invoke(stock);
    }
}
