namespace Ordering.Orders.Features.DeleteOrder;

public record DeleteOrderCommand(Guid OrderId, Guid CustomerId, bool CanDeleteAll)
    : ICommand<DeleteOrderResult>;
public record DeleteOrderResult(bool IsSuccess);
public class DeleteOrderCommandValidator : AbstractValidator<DeleteOrderCommand>
{
    public DeleteOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderName is required");
    }
}

internal class DeleteOrderHandler(OrderingDbContext dbContext)
    : ICommandHandler<DeleteOrderCommand, DeleteOrderResult>
{
    public async Task<DeleteOrderResult> Handle(DeleteOrderCommand command, CancellationToken cancellationToken)
    {
        IQueryable<Order> orders = dbContext.Orders;
        if (!command.CanDeleteAll)
        {
            orders = orders.Where(order => order.CustomerId == command.CustomerId);
        }

        var order = await orders.SingleOrDefaultAsync(
            order => order.Id == command.OrderId,
            cancellationToken);

        if (order is null)
        {
            throw new OrderNotFoundException(command.OrderId);
        }

        dbContext.Orders.Remove(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteOrderResult(true);
    }
}
