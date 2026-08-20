namespace Ordering.Orders.Dtos;
public record AddressDto(
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
