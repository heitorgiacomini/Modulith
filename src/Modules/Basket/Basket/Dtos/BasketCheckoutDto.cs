namespace Basket.Basket.Dtos;
public record BasketCheckoutDto(
  string UserName,
  Guid CustomerId,
  decimal TotalPrice,
  BasketCheckoutAddressDto Address,
  BasketCheckoutPaymentDto Payment);

public record BasketCheckoutAddressDto(
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

public record BasketCheckoutPaymentDto(
  string Token,
  string CardholderName,
  string Brand,
  string Last4,
  string Expiration);
