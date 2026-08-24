namespace Ordering.Orders.Features.GetOrderById;

public record GetOrderByIdQuery(Guid Id)
    : IQuery<GetOrderByIdResult>;
public record GetOrderByIdResult(OrderDto Order);

internal class GetOrderByIdHandler(OrderingDbContext dbContext, IOrderingPermissionEvaluator evaluator)
    : IQueryHandler<GetOrderByIdQuery, GetOrderByIdResult>
{
    public async Task<GetOrderByIdResult> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Order> orders = dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items);

        OrderingPermission permission = evaluator.Evaluate()
            ?? throw new ForbiddenException("Ordering read permission is required.");
        if (!permission.HasScope(OrderingAuthorization.ReadAllScope))
        {
            orders = orders.Where(order => order.CustomerId == permission.CustomerId);
        }

        var order = await orders.SingleOrDefaultAsync(
            order => order.Id == query.Id,
            cancellationToken);

        if (order is null)
        {
            throw new OrderNotFoundException(query.Id);
        }

        var orderDto = OrderingMapper.ToDto(order);

        return new GetOrderByIdResult(orderDto);
    }
}
