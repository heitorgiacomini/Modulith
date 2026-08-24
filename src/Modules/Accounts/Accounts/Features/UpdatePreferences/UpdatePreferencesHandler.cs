namespace Accounts.Accounts.Features.UpdatePreferences;

public record UpdatePreferencesCommand(Guid CustomerId, PreferencesDto Preferences) : ICommand<AccountDto>;

public sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
	public UpdatePreferencesCommandValidator()
	{
		RuleFor(command => command.Preferences.Locale).NotEmpty().MaximumLength(10);
		RuleFor(command => command.Preferences.Currency).NotEmpty().Length(3);
	}
}

internal sealed class UpdatePreferencesHandler(AccountsDbContext dbContext)
	: ICommandHandler<UpdatePreferencesCommand, AccountDto>
{
	public async Task<AccountDto> Handle(UpdatePreferencesCommand command, CancellationToken cancellationToken)
	{
		CustomerAccount account = await LoadOrCreate(command.CustomerId, cancellationToken);
		PreferencesDto preferences = command.Preferences;
		account.UpdatePreferences(
			preferences.Locale,
			preferences.Currency.ToUpperInvariant(),
			preferences.OrderStatusNotifications,
			preferences.MarketingEmails);
		await dbContext.SaveChangesAsync(cancellationToken);
		return AccountMapping.ToDto(account);
	}

	private async Task<CustomerAccount> LoadOrCreate(Guid customerId, CancellationToken cancellationToken)
	{
		CustomerAccount? account = await dbContext.CustomerAccounts
			.Include(item => item.Addresses)
			.Include(item => item.PaymentMethods)
			.SingleOrDefaultAsync(item => item.UserId == customerId, cancellationToken);
		if (account is not null)
		{
			return account;
		}

		account = CustomerAccount.Create(customerId);
		dbContext.CustomerAccounts.Add(account);
		return account;
	}
}
