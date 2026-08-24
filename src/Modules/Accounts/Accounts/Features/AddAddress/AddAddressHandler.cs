namespace Accounts.Accounts.Features.AddAddress;

public record AddAddressCommand(Guid CustomerId, SaveAddressDto Address) : ICommand<SavedAddressDto>;

public sealed class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
	public AddAddressCommandValidator() => RuleFor(command => command.Address).SetValidator(new SaveAddressValidator());
}

internal sealed class SaveAddressValidator : AbstractValidator<SaveAddressDto>
{
	public SaveAddressValidator()
	{
		RuleFor(address => address.Label).NotEmpty().MaximumLength(40);
		RuleFor(address => address.FirstName).NotEmpty().MaximumLength(50);
		RuleFor(address => address.LastName).NotEmpty().MaximumLength(50);
		RuleFor(address => address.Email).NotEmpty().EmailAddress().MaximumLength(100);
		RuleFor(address => address.Phone).NotEmpty().MaximumLength(30);
		RuleFor(address => address.AddressLine1).NotEmpty().MaximumLength(180);
		RuleFor(address => address.AddressLine2).MaximumLength(180);
		RuleFor(address => address.City).NotEmpty().MaximumLength(80);
		RuleFor(address => address.State).NotEmpty().MaximumLength(80);
		RuleFor(address => address.PostalCode).NotEmpty().MaximumLength(20);
		RuleFor(address => address.CountryCode).NotEmpty().Length(2);
	}
}

internal sealed class AddAddressHandler(AccountsDbContext dbContext)
	: ICommandHandler<AddAddressCommand, SavedAddressDto>
{
	public async Task<SavedAddressDto> Handle(AddAddressCommand command, CancellationToken cancellationToken)
	{
		CustomerAccount? account = await dbContext.CustomerAccounts
			.Include(item => item.Addresses)
			.SingleOrDefaultAsync(item => item.UserId == command.CustomerId, cancellationToken);
		if (account is null)
		{
      account = CustomerAccount.Create(command.CustomerId);
			dbContext.CustomerAccounts.Add(account);
		}

		SavedAddress address = account.AddAddress(AccountMapping.ToData(command.Address));
		dbContext.SavedAddresses.Add(address);
		await dbContext.SaveChangesAsync(cancellationToken);
		return AccountMapping.ToDto(address);
	}
}
