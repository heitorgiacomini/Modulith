namespace Shared.Messaging.Events;
public record BasketCheckoutIntegrationEvent : IntegrationEvent
{
    public string UserName { get; set; } = default!;
    public Guid CustomerId { get; set; } = default!;
    public decimal TotalPrice { get; set; } = default!;

    public BasketCheckoutAddress Address { get; set; } = default!;
    public BasketCheckoutPayment Payment { get; set; } = default!;
    public List<BasketCheckoutItem> Items { get; set; } = [];
}

public record BasketCheckoutItem(Guid ProductId, int Quantity, decimal Price);

public record BasketCheckoutAddress(
    string FirstName,
    string LastName,
    string EmailAddress,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string CountryCode);

public record BasketCheckoutPayment(
    string Token,
    string CardholderName,
    string Brand,
    string Last4,
    string Expiration);
