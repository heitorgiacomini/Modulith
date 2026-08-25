using Accounts.Accounts.Domain;
using Accounts.Accounts.Dtos;
using Accounts.Accounts.Mapping;
using Basket.Basket.Domain;
using Basket.Basket.Mapping;
using Catalog.Products.Domain;
using Catalog.Products.Mapping;
using Ordering.Orders.Domain;
using Ordering.Orders.Features.CreateOrder;
using Ordering.Orders.Mapping;
using Ordering.Orders.ValueObjects;
using Shared.Messaging.Events;
using Xunit;

namespace ArchitectureTests;

public sealed class MappingContractTests
{
    [Fact]
    public void AccountMapper_MapsNestedCollectionsAndDefaults()
    {
        CustomerAccount account = CustomerAccount.Create(Guid.NewGuid());
        account.UpdatePreferences("pt-BR", "BRL", false, true);
        account.AddAddress(new AddressData(
            "Home", "Ada", "Lovelace", "ada@example.test", "+55", "One Street", null,
            "Sao Paulo", "SP", "01000-000", "br", true, false));

        AccountDto dto = AccountMapper.ToDto(account);
        AddressData roundTrip = AccountMapper.ToDomain(new SaveAddressDto(
            "Work", "Grace", "Hopper", "grace@example.test", "+1", "Two Street", null,
            "New York", "NY", "10001", "US", false, true));

        Assert.Equal("pt-BR", dto.Preferences.Locale);
        Assert.Single(dto.Addresses);
        Assert.Equal("Work", roundTrip.Label);
        Assert.Empty(AccountDtoDefaults.Empty().Addresses);
    }

    [Fact]
    public void DomainMappers_PreserveNestedDtoShapes()
    {
        Product product = Product.Create(Guid.NewGuid(), "Keyboard", ["hardware"], "Mechanical", "keyboard.png", 99m);
        ShoppingCart cart = ShoppingCart.Create(Guid.NewGuid(), "ada");
        cart.AddItem(product.Id, 2, "black", product.Price, product.Name);
        Address address = Address.Of("Ada", "Lovelace", "ada@example.test", "+1", "One Street", null, "London", "LN", "12345", "GB");
        Payment payment = Payment.Of("token", "Ada Lovelace", "visa", "4242", "12/30");
        Order order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), "order", address, address, payment);
        order.Add(product.Id, 2, product.Price);

        Assert.Equal(product.Id, CatalogMapper.ToDto(product).Id);
        Assert.Single(BasketMapper.ToDto(cart).Items);
        Assert.Single(OrderingMapper.ToDto(order).Items);
    }

    [Fact]
    public void OrderingInputMapper_SuppliesInternalIdentifiers()
    {
        CreateOrderInput input = new(
            "order",
            new("Ada", "Lovelace", "ada@example.test", "+1", "One Street", null, "London", "LN", "12345", "GB"),
            new("Ada", "Lovelace", "ada@example.test", "+1", "One Street", null, "London", "LN", "12345", "GB"),
            new("token", "Ada Lovelace", "visa", "4242", "12/30"),
            [new(Guid.NewGuid(), 1, 10m)]);

        var dto = OrderingMapper.ToDto(input);

        Assert.Equal(Guid.Empty, dto.Id);
        Assert.Equal(Guid.Empty, dto.CustomerId);
        Assert.Equal(Guid.Empty, dto.Items.Single().OrderId);
    }

    [Fact]
    public void OrderingMapper_MapsCommandsAndCreatesAggregate()
    {
        CreateOrderInput input = new(
            "order",
            new("Ada", "Lovelace", "ada@example.test", "+1", "One Street", null, "London", "LN", "12345", "GB"),
            new("Ada", "Lovelace", "ada@example.test", "+1", "One Street", null, "London", "LN", "12345", "GB"),
            new("token", "Ada Lovelace", "visa", "4242", "12/30"),
            [new(Guid.NewGuid(), 1, 10m)]);

        CreateOrderCommand command = OrderingMapper.ToCommand(new CreateOrderRequest(input));
        Order order = OrderingMapper.ToDomain(command.Order with { CustomerId = Guid.NewGuid() });

        Assert.StartsWith("order_", order.OrderName);
        Assert.Single(order.Items);

        BasketCheckoutIntegrationEvent integrationEvent = new()
        {
            CustomerId = Guid.NewGuid(),
            UserName = "ada",
            Address = new("Ada", "Lovelace", "ada@example.test", "+1", "One Street", null, "London", "LN", "12345", "GB"),
            Payment = new("token", "Ada Lovelace", "visa", "4242", "12/30"),
            Items = [new(Guid.NewGuid(), 2, 20m)],
        };

        CreateOrderFromCheckoutCommand checkoutCommand = OrderingMapper.ToCommand(integrationEvent);

        Assert.Equal(integrationEvent.CustomerId, checkoutCommand.Order.CustomerId);
        Assert.Equal(integrationEvent.UserName, checkoutCommand.Order.OrderName);
        Assert.Single(checkoutCommand.Order.Items);
    }
}
