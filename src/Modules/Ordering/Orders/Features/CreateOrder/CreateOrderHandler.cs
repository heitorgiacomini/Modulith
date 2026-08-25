namespace Ordering.Orders.Features.CreateOrder;

public record CreateOrderCommand(OrderDto Order)
    : ICommand<CreateOrderResult>;
public record CreateOrderResult(Guid Id);
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Order.OrderName).NotEmpty().WithMessage("OrderName is required");
    }
}

internal class CreateOrderHandler(OrderingDbContext dbContext, IOrderingPermissionEvaluator evaluator)
    : ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        OrderingPermission permission = evaluator.Evaluate()
            ?? throw new ForbiddenException("Ordering create permission is required.");
        var order = OrderingMapper.ToDomain(command.Order with { CustomerId = permission.CustomerId });

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateOrderResult(order.Id);
    }
}
