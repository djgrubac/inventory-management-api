using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Models;
using Inventory_Management.Application.Common.Security;

namespace Microsoft.Extensions.DependencyInjection.Order.Queries.GetCollection;

public class GetOrderItemsByOrderUidQueryHandler : IRequestHandler<GetOrderItemsByOrderUidQuery, IEnumerable<OrderItemDTO>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserAccess _access;

    public GetOrderItemsByOrderUidQueryHandler(IOrderRepository orderRepository, ICurrentUserAccess access)
    {
        _orderRepository = orderRepository;
        _access = access;
    }

    public async Task<IEnumerable<OrderItemDTO>> Handle(GetOrderItemsByOrderUidQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByUidAsync(request.OrderUid);
        if (order == null || !_access.CanRead(order.WarehouseId))
            return Enumerable.Empty<OrderItemDTO>();

        var projector = OrderItemDTO.Projection.Compile();
        return order.Items.Select(projector);
    }
}

