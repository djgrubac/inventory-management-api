using Inventory_Management.Application.Common.Models;
using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Security;

namespace Microsoft.Extensions.DependencyInjection.Order.Queries.GetCollection;

public class GetOrdersByWarehouseQueryHandler : IRequestHandler<GetOrdersByWarehouseQuery, IEnumerable<OrderDTO>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserAccess _access;

    public GetOrdersByWarehouseQueryHandler(IOrderRepository orderRepository, ICurrentUserAccess access)
    {
        _orderRepository = orderRepository;
        _access = access;
    }

    public async Task<IEnumerable<OrderDTO>> Handle(GetOrdersByWarehouseQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetByWarehouseUidAsync(request.WarehouseUid);

        var projector = OrderDTO.Projection.Compile();
        return orders.Where(o => _access.CanRead(o.WarehouseId)).Select(projector);
    }
}

