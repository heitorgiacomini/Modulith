namespace Accounts.Accounts.Features.SetDefaultPaymentMethod;

public record SetDefaultPaymentMethodCommand(Guid CustomerId, Guid PaymentMethodId)
	: ICommand<SetDefaultPaymentMethodResult>;

public record SetDefaultPaymentMethodResult(SavedPaymentMethodDto? PaymentMethod);

internal sealed class SetDefaultPaymentMethodHandler(AccountsDbContext dbContext)
	: ICommandHandler<SetDefaultPaymentMethodCommand, SetDefaultPaymentMethodResult>
{
	public async Task<SetDefaultPaymentMethodResult> Handle(
		SetDefaultPaymentMethodCommand command,
		CancellationToken cancellationToken)
	{
		CustomerAccount? account = await dbContext.CustomerAccounts
			.Include(item => item.PaymentMethods)
			.SingleOrDefaultAsync(item => item.UserId == command.CustomerId, cancellationToken);
		if (account is null || !account.SetDefaultPaymentMethod(command.PaymentMethodId))
		{
			return new SetDefaultPaymentMethodResult(null);
		}

		await dbContext.SaveChangesAsync(cancellationToken);
		return new SetDefaultPaymentMethodResult(
			AccountMapping.ToDto(account.PaymentMethods.Single(item => item.Id == command.PaymentMethodId)));
	}
}
