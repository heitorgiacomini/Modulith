namespace Ordering.Orders.Features.GetOrderById;

public record GetOrderByIdQuery(Guid Id, Guid CustomerId, bool CanReadAll)
    : IQuery<GetOrderByIdResult>;
public record GetOrderByIdResult(OrderDto Order);

internal class GetOrderByIdHandler(OrderingDbContext dbContext)
    : IQueryHandler<GetOrderByIdQuery, GetOrderByIdResult>
{
    public async Task<GetOrderByIdResult> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Order> orders = dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items);

        if (!query.CanReadAll)
        {
            orders = orders.Where(order => order.CustomerId == query.CustomerId);
        }

        var order = await orders.SingleOrDefaultAsync(
            order => order.Id == query.Id,
            cancellationToken);

        if (order is null)
        {
            throw new OrderNotFoundException(query.Id);
        }

        var orderDto = order.Adapt<OrderDto>();

        return new GetOrderByIdResult(orderDto);
    }
}
