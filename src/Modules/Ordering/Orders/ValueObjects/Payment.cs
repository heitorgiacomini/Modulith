namespace Ordering.Orders.ValueObjects;
public record Payment
{
    public string Token { get; } = default!;
    public string CardholderName { get; } = default!;
    public string Brand { get; } = default!;
    public string Last4 { get; } = default!;
    public string Expiration { get; } = default!;

    protected Payment()
    {
    }

    private Payment(string token, string cardholderName, string brand, string last4, string expiration)
    {
        Token = token;
        CardholderName = cardholderName;
        Brand = brand;
        Last4 = last4;
        Expiration = expiration;
    }

    public static Payment Of(string token, string cardholderName, string brand, string last4, string expiration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(cardholderName);
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentException.ThrowIfNullOrWhiteSpace(last4);

        return new Payment(token, cardholderName, brand, last4, expiration);
    }
}
