namespace Ordering.Orders.Features.CreateOrder;

internal record CreateOrderFromCheckoutCommand(OrderDto Order)
    : ICommand<CreateOrderFromCheckoutResult>;

internal record CreateOrderFromCheckoutResult(Guid Id);

internal sealed class CreateOrderFromCheckoutCommandValidator
    : AbstractValidator<CreateOrderFromCheckoutCommand>
{
    public CreateOrderFromCheckoutCommandValidator()
    {
        RuleFor(command => command.Order.CustomerId).NotEmpty().WithMessage("CustomerId is required");
        RuleFor(command => command.Order.OrderName).NotEmpty().WithMessage("OrderName is required");
    }
}

internal sealed class CreateOrderFromCheckoutHandler(OrderingDbContext dbContext)
    : ICommandHandler<CreateOrderFromCheckoutCommand, CreateOrderFromCheckoutResult>
{
    public async Task<CreateOrderFromCheckoutResult> Handle(
        CreateOrderFromCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        Order order = OrderingMapper.ToDomain(command.Order);

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateOrderFromCheckoutResult(order.Id);
    }
}
