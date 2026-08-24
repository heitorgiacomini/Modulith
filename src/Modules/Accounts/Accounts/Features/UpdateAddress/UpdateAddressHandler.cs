namespace Accounts.Accounts.Features.UpdateAddress;

public record UpdateAddressCommand(Guid CustomerId, Guid AddressId, SaveAddressDto Address)
	: ICommand<UpdateAddressResult>;

public record UpdateAddressResult(SavedAddressDto? Address);

public sealed class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
	public UpdateAddressCommandValidator()
	{
		RuleFor(command => command.AddressId).NotEmpty();
		RuleFor(command => command.Address).SetValidator(new SaveAddressValidator());
	}

	private sealed class SaveAddressValidator : AbstractValidator<SaveAddressDto>
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
}

internal sealed class UpdateAddressHandler(AccountsDbContext dbContext)
	: ICommandHandler<UpdateAddressCommand, UpdateAddressResult>
{
	public async Task<UpdateAddressResult> Handle(UpdateAddressCommand command, CancellationToken cancellationToken)
	{
		CustomerAccount? account = await dbContext.CustomerAccounts
			.Include(item => item.Addresses)
			.SingleOrDefaultAsync(item => item.UserId == command.CustomerId, cancellationToken);
		if (account is null || !account.UpdateAddress(command.AddressId, AccountMapper.ToDomain(command.Address)))
		{
			return new UpdateAddressResult(null);
		}

		await dbContext.SaveChangesAsync(cancellationToken);
		return new UpdateAddressResult(AccountMapper.ToDto(account.Addresses.Single(item => item.Id == command.AddressId)));
	}
}
