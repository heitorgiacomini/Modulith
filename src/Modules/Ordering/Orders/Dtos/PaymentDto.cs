namespace Ordering.Orders.Dtos;
public record PaymentDto(string Token, string CardholderName, string Brand, string Last4, string Expiration);
