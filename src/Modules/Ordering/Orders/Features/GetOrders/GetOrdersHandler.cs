namespace Ordering.Orders.Features.GetOrders;

public record GetOrdersQuery(PaginationRequest PaginationRequest)
    : IQuery<GetOrdersResult>;
public record GetOrdersResult(PaginatedResult<OrderDto> Orders);

internal class GetOrdersHandler(OrderingDbContext dbContext, IOrderingPermissionEvaluator evaluator)
    : IQueryHandler<GetOrdersQuery, GetOrdersResult>
{
    public async Task<GetOrdersResult> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        var pageIndex = query.PaginationRequest.PageIndex;
        var pageSize = query.PaginationRequest.PageSize;

        OrderingPermission permission = evaluator.Evaluate()
            ?? throw new ForbiddenException("Ordering read permission is required.");
        IQueryable<Order> customerOrders = dbContext.Orders;
        if (!permission.HasScope(OrderingAuthorization.ReadAllScope))
        {
            customerOrders = customerOrders.Where(order => order.CustomerId == permission.CustomerId);
        }

        var totalCount = await customerOrders.LongCountAsync(cancellationToken);

        var orders = await customerOrders
                        .AsNoTracking()
                        .Include(x => x.Items)
                        .OrderBy(p => p.OrderName)
                        .Skip(pageSize * pageIndex)
                        .Take(pageSize)
                        .ToListAsync(cancellationToken);

        var orderDtos = OrderingMapper.ToDtos(orders);

        return new GetOrdersResult(
            new PaginatedResult<OrderDto>(
                pageIndex,
                pageSize,
                totalCount,
                orderDtos));
    }
}
