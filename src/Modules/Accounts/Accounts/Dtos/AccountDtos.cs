namespace Accounts.Accounts.Dtos;

public sealed record AccountDto(
  PreferencesDto Preferences,
  IReadOnlyCollection<SavedAddressDto> Addresses,
  IReadOnlyCollection<SavedPaymentMethodDto> PaymentMethods);

public sealed record PreferencesDto(
  string Locale,
  string Currency,
  bool OrderStatusNotifications,
  bool MarketingEmails);

public sealed record SavedAddressDto(
  Guid Id,
  string Label,
  string FirstName,
  string LastName,
  string Email,
  string Phone,
  string AddressLine1,
  string? AddressLine2,
  string City,
  string State,
  string PostalCode,
  string CountryCode,
  bool IsDefaultShipping,
  bool IsDefaultBilling);

public sealed record SaveAddressDto(
  string Label,
  string FirstName,
  string LastName,
  string Email,
  string Phone,
  string AddressLine1,
  string? AddressLine2,
  string City,
  string State,
  string PostalCode,
  string CountryCode,
  bool IsDefaultShipping,
  bool IsDefaultBilling);

public sealed record SavedPaymentMethodDto(
  Guid Id,
  string Label,
  string CardholderName,
  string Brand,
  string Last4,
  string Expiration,
  string Token,
  bool IsDefault);

public sealed record SavePaymentMethodDto(
  string Label,
  string CardholderName,
  string Brand,
  string Last4,
  string Expiration,
  string Token,
  bool IsDefault);
