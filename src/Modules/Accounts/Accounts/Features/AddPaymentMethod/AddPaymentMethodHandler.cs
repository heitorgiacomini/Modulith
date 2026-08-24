namespace Accounts.Accounts.Features.AddPaymentMethod;

public record AddPaymentMethodCommand(Guid CustomerId, SavePaymentMethodDto PaymentMethod)
	: ICommand<SavedPaymentMethodDto>;

public sealed class AddPaymentMethodCommandValidator : AbstractValidator<AddPaymentMethodCommand>
{
	public AddPaymentMethodCommandValidator()
		=> RuleFor(command => command.PaymentMethod).SetValidator(new SavePaymentMethodValidator());

	private sealed class SavePaymentMethodValidator : AbstractValidator<SavePaymentMethodDto>
	{
		public SavePaymentMethodValidator()
		{
			RuleFor(paymentMethod => paymentMethod.Label).NotEmpty().MaximumLength(40);
			RuleFor(paymentMethod => paymentMethod.CardholderName).NotEmpty().MaximumLength(100);
			RuleFor(paymentMethod => paymentMethod.Brand).NotEmpty().MaximumLength(30);
			RuleFor(paymentMethod => paymentMethod.Last4).Matches("^[0-9]{4}$");
			RuleFor(paymentMethod => paymentMethod.Expiration).NotEmpty().MaximumLength(10);
			RuleFor(paymentMethod => paymentMethod.Token).NotEmpty().MaximumLength(200);
		}
	}
}

internal sealed class AddPaymentMethodHandler(AccountsDbContext dbContext)
	: ICommandHandler<AddPaymentMethodCommand, SavedPaymentMethodDto>
{
	public async Task<SavedPaymentMethodDto> Handle(AddPaymentMethodCommand command, CancellationToken cancellationToken)
	{
		CustomerAccount? account = await dbContext.CustomerAccounts
			.Include(item => item.PaymentMethods)
			.SingleOrDefaultAsync(item => item.UserId == command.CustomerId, cancellationToken);
		if (account is null)
		{
			account = CustomerAccount.Create(command.CustomerId);
			dbContext.CustomerAccounts.Add(account);
		}

		SavedPaymentMethod paymentMethod = account.AddPaymentMethod(AccountMapper.ToDomain(command.PaymentMethod));
		dbContext.SavedPaymentMethods.Add(paymentMethod);
		await dbContext.SaveChangesAsync(cancellationToken);
		return AccountMapper.ToDto(paymentMethod);
	}
}
