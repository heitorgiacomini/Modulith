using Microsoft.EntityFrameworkCore;
using Ordering.Data;
using Ordering.Orders.Domain;
using Ordering.Orders.Dtos;
using Ordering.Orders.Features.CreateOrder;
using Shared.Data.Filtering;
using Shared.Data.Interceptors;
using Shared.Data.MultiTenancy;
using Xunit;

namespace OrderingTests;

public sealed class CheckoutOrderCreationTests
{
    [Fact]
    public async Task Trusted_checkout_command_creates_order_without_http_permission_context()
    {
        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid customerId = Guid.NewGuid();
        CurrentTenant currentTenant = new();
        using IDisposable tenantScope = currentTenant.Change(tenantId);
        DbContextOptions<OrderingDbContext> options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new MultiTenantEntityInterceptor(currentTenant))
            .Options;
        await using OrderingDbContext dbContext = new(options, new EnabledDataFilter(), currentTenant);
        CreateOrderFromCheckoutHandler handler = new(dbContext);
        OrderDto order = CreateOrder(customerId);

        CreateOrderFromCheckoutResult result = await handler.Handle(
            new CreateOrderFromCheckoutCommand(order),
            CancellationToken.None);

        var persisted = Assert.Single(dbContext.ChangeTracker.Entries<Order>()).Entity;
        Assert.Equal(result.Id, persisted.Id);
        Assert.Equal(customerId, persisted.CustomerId);
        Assert.Equal(tenantId, persisted.TenantId);
        Assert.Single(persisted.Items);
    }

    private static OrderDto CreateOrder(Guid customerId)
    {
        Guid orderId = Guid.NewGuid();
        AddressDto address = new(
            "Local",
            "Shopper",
            "shopper@example.test",
            "555-0100",
            "1 Main Street",
            null,
            "Sao Paulo",
            "SP",
            "01000-000",
            "BR");
        PaymentDto payment = new("tok_test", "Local Shopper", "Visa", "4242", "12/30");
        List<OrderItemDto> items =
        [
            new(orderId, Guid.NewGuid(), 1, 500m),
        ];
        return new OrderDto(orderId, customerId, "shopper", address, address, payment, items);
    }

    private sealed class EnabledDataFilter : IDataFilter
    {
        public IDisposable Enable<TFilter>() where TFilter : class => EmptyScope.Instance;
        public IDisposable Disable<TFilter>() where TFilter : class => EmptyScope.Instance;
        public bool IsEnabled<TFilter>() where TFilter : class => true;

        private sealed class EmptyScope : IDisposable
        {
            public static EmptyScope Instance { get; } = new();
            public void Dispose()
            {
            }
        }
    }
}
