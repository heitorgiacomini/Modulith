namespace Accounts.Accounts.Mapping;

internal static class AccountMapping
{
	public static AccountDto Empty() => new(
		new PreferencesDto("en-US", "USD", true, false),
		[],
		[]);

	public static AccountDto ToDto(CustomerAccount account) => new(
		new PreferencesDto(account.Locale, account.Currency, account.OrderStatusNotifications, account.MarketingEmails),
		account.Addresses.Select(ToDto).ToArray(),
		account.PaymentMethods.Select(ToDto).ToArray());

	public static SavedAddressDto ToDto(SavedAddress address) => new(
		address.Id, address.Label, address.FirstName, address.LastName, address.Email, address.Phone,
		address.AddressLine1, address.AddressLine2, address.City, address.State, address.PostalCode,
		address.CountryCode, address.IsDefaultShipping, address.IsDefaultBilling);

	public static AddressData ToData(SaveAddressDto address) => new(
		address.Label, address.FirstName, address.LastName, address.Email, address.Phone,
		address.AddressLine1, address.AddressLine2, address.City, address.State, address.PostalCode,
		address.CountryCode, address.IsDefaultShipping, address.IsDefaultBilling);

	public static SavedPaymentMethodDto ToDto(SavedPaymentMethod paymentMethod) => new(
		paymentMethod.Id, paymentMethod.Label, paymentMethod.CardholderName, paymentMethod.Brand,
		paymentMethod.Last4, paymentMethod.Expiration, paymentMethod.Token, paymentMethod.IsDefault);

	public static PaymentMethodData ToData(SavePaymentMethodDto paymentMethod) => new(
		paymentMethod.Label, paymentMethod.CardholderName, paymentMethod.Brand, paymentMethod.Last4,
		paymentMethod.Expiration, paymentMethod.Token, paymentMethod.IsDefault);
}
