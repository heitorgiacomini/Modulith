namespace Accounts.Accounts.Features.DeletePaymentMethod;

public record DeletePaymentMethodCommand(Guid CustomerId, Guid PaymentMethodId) : ICommand<bool>;

internal sealed class DeletePaymentMethodHandler(AccountsDbContext dbContext)
	: ICommandHandler<DeletePaymentMethodCommand, bool>
{
	public async Task<bool> Handle(DeletePaymentMethodCommand command, CancellationToken cancellationToken)
	{
		CustomerAccount? account = await dbContext.CustomerAccounts
			.Include(item => item.PaymentMethods)
			.SingleOrDefaultAsync(item => item.UserId == command.CustomerId, cancellationToken);
		if (account is null || !account.RemovePaymentMethod(command.PaymentMethodId))
		{
			return false;
		}

		await dbContext.SaveChangesAsync(cancellationToken);
		return true;
	}
}
