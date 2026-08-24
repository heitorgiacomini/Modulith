namespace Ordering.Orders.Features.DeleteOrder;

public record DeleteOrderCommand(Guid OrderId)
    : ICommand<DeleteOrderResult>;
public record DeleteOrderResult(bool IsSuccess);
public class DeleteOrderCommandValidator : AbstractValidator<DeleteOrderCommand>
{
    public DeleteOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderName is required");
    }
}

internal class DeleteOrderHandler(OrderingDbContext dbContext, IOrderingPermissionEvaluator evaluator)
    : ICommandHandler<DeleteOrderCommand, DeleteOrderResult>
{
    public async Task<DeleteOrderResult> Handle(DeleteOrderCommand command, CancellationToken cancellationToken)
    {
        OrderingPermission permission = evaluator.Evaluate()
            ?? throw new ForbiddenException("Ordering delete permission is required.");
        IQueryable<Order> orders = dbContext.Orders;
        if (!permission.HasScope(OrderingAuthorization.DeleteAllScope))
        {
            orders = orders.Where(order => order.CustomerId == permission.CustomerId);
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
