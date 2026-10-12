namespace Ordering.Orders.Features.CreateOrder;

internal record CreateOrderFromCheckoutCommand(OrderDto Order, Guid EventId)
    : ICommand<CreateOrderFromCheckoutResult>;

internal record CreateOrderFromCheckoutResult(Guid Id);

internal sealed class CreateOrderFromCheckoutCommandValidator
    : AbstractValidator<CreateOrderFromCheckoutCommand>
{
    public CreateOrderFromCheckoutCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.Order.CustomerId).NotEmpty().WithMessage("CustomerId is required");
        RuleFor(command => command.Order.OrderName).NotEmpty().WithMessage("OrderName is required");
    }
}

internal sealed class CreateOrderFromCheckoutHandler(OrderingDbContext dbContext, ICurrentTenant currentTenant)
    : ICommandHandler<CreateOrderFromCheckoutCommand, CreateOrderFromCheckoutResult>
{
    public async Task<CreateOrderFromCheckoutResult> Handle(
        CreateOrderFromCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        if (command.EventId == Guid.Empty)
            throw new InvalidOperationException("Checkout event ID is required.");
        Guid tenantId = currentTenant.Id ?? throw new InvalidOperationException("An active tenant is required.");
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (dbContext.Database.IsNpgsql())
        {
            // Serialize competing deliveries of the same checkout, including across replicas.
            string key = $"{tenantId}:{command.EventId}";
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        }
        CheckoutReceipt? existing = await dbContext.Set<CheckoutReceipt>()
            .SingleOrDefaultAsync(receipt => receipt.TenantId == tenantId && receipt.EventId == command.EventId, cancellationToken);
        if (existing is not null)
            return new CreateOrderFromCheckoutResult(existing.OrderId);

        Order order = OrderingMapper.ToDomain(command.Order);

        dbContext.Orders.Add(order);
        dbContext.Add(new CheckoutReceipt { TenantId = tenantId, EventId = command.EventId, OrderId = order.Id });
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return new CreateOrderFromCheckoutResult(order.Id);
    }
}
