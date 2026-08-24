using Riok.Mapperly.Abstractions;

namespace Accounts.Accounts.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class AccountMapper
{
	[MapPropertyFromSource(nameof(AccountDto.Preferences), Use = nameof(ToPreferences))]
	public static partial AccountDto ToDto(CustomerAccount account);

	private static PreferencesDto ToPreferences(CustomerAccount account) => new(
		account.Locale,
		account.Currency,
		account.OrderStatusNotifications,
		account.MarketingEmails);

	public static partial SavedAddressDto ToDto(SavedAddress address);

	public static partial AddressData ToDomain(SaveAddressDto address);

	public static partial SavedPaymentMethodDto ToDto(SavedPaymentMethod paymentMethod);

	public static partial PaymentMethodData ToDomain(SavePaymentMethodDto paymentMethod);
}

internal static class AccountDtoDefaults
{
	public static AccountDto Empty() => new(
		new PreferencesDto("en-US", "USD", true, false),
		[],
		[]);
}
