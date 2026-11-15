using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Models;
using Inventory_Management.Application.Common.Security;

namespace Microsoft.Extensions.DependencyInjection.Order.Queries.GetSingle;

public class GetOrderByUidQueryHandler : IRequestHandler<GetOrderByUidQuery, OrderDTO?>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserAccess _access;

    public GetOrderByUidQueryHandler(IOrderRepository orderRepository, ICurrentUserAccess access)
    {
        _orderRepository = orderRepository;
        _access = access;
    }

    public async Task<OrderDTO?> Handle(GetOrderByUidQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByUidAsync(request.Uid);
        // An order in a warehouse the caller can't read looks the same as a missing one.
        if (order == null || !_access.CanRead(order.WarehouseId))
            return null;

        var projector = OrderDTO.Projection.Compile();
        return projector(order);
    }
}

