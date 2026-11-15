using Inventory_Management.Application.Common.Interfaces;
using Inventory_Management.Application.Common.Security;
using Inventory_Management.Domain.Enums;
using InventoryManagement.Core.Interfaces;
using Entities = Inventory_Management.Domain.Entities;

namespace Microsoft.Extensions.DependencyInjection.Order.Commands.Create;

public class OrderCreateCommandHandler : IRequestHandler<OrderCreateCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IBaseRepository<Entities.Warehouse> _warehouseRepository;
    private readonly ICurrentUserAccess _access;

    public OrderCreateCommandHandler(
        IOrderRepository orderRepository,
        IBaseRepository<Entities.Warehouse> warehouseRepository,
        ICurrentUserAccess access)
    {
        _orderRepository = orderRepository;
        _warehouseRepository = warehouseRepository;
        _access = access;
    }

    public async Task<Guid> Handle(OrderCreateCommand request, CancellationToken cancellationToken)
    {
        // Validate warehouse exists
        var warehouse = await _warehouseRepository.GetByUidAsync(request.WarehouseUid);
        if (warehouse == null)
        {
            throw new ArgumentException($"Warehouse with UID {request.WarehouseUid} not found.");
        }
        _access.EnsureCanWrite(warehouse.Id, request.WarehouseUid.ToString(), "Warehouse");

        // Create new order
        var order = new Entities.Order
        {
            Uid = Guid.NewGuid(),
            Type = request.Type,
            Status = OrderStatus.Pending, // Assuming initial status
            Warehouse = warehouse,
            TotalPrice = 0m, // Will be calculated when items are added
            Items = new List<Entities.OrderItem>(),
            Created = DateTimeOffset.UtcNow
        };

        await _orderRepository.AddAsync(order);

        return order.Uid;
    }
}

