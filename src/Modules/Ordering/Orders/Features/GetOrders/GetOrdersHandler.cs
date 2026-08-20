namespace Ordering.Orders.Features.GetOrders;

public record GetOrdersQuery(Guid? CustomerId, PaginationRequest PaginationRequest)
    : IQuery<GetOrdersResult>;
public record GetOrdersResult(PaginatedResult<OrderDto> Orders);

internal class GetOrdersHandler(OrderingDbContext dbContext)
    : IQueryHandler<GetOrdersQuery, GetOrdersResult>
{
    public async Task<GetOrdersResult> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        var pageIndex = query.PaginationRequest.PageIndex;
        var pageSize = query.PaginationRequest.PageSize;

        IQueryable<Order> customerOrders = dbContext.Orders;
        if (query.CustomerId is { } customerId)
        {
            customerOrders = customerOrders.Where(order => order.CustomerId == customerId);
        }

        var totalCount = await customerOrders.LongCountAsync(cancellationToken);

        var orders = await customerOrders
                        .AsNoTracking()
                        .Include(x => x.Items)
                        .OrderBy(p => p.OrderName)
                        .Skip(pageSize * pageIndex)
                        .Take(pageSize)
                        .ToListAsync(cancellationToken);

        var orderDtos = orders.Adapt<List<OrderDto>>();

        return new GetOrdersResult(
            new PaginatedResult<OrderDto>(
                pageIndex,
                pageSize,
                totalCount,
                orderDtos));
    }
}
