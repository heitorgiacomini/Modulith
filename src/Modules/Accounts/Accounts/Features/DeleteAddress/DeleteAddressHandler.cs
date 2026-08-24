namespace Accounts.Accounts.Features.DeleteAddress;

public record DeleteAddressCommand(Guid CustomerId, Guid AddressId) : ICommand<bool>;

internal sealed class DeleteAddressHandler(AccountsDbContext dbContext)
	: ICommandHandler<DeleteAddressCommand, bool>
{
	public async Task<bool> Handle(DeleteAddressCommand command, CancellationToken cancellationToken)
	{
		CustomerAccount? account = await dbContext.CustomerAccounts
			.Include(item => item.Addresses)
			.SingleOrDefaultAsync(item => item.UserId == command.CustomerId, cancellationToken);
		if (account is null || !account.RemoveAddress(command.AddressId))
		{
			return false;
		}

		await dbContext.SaveChangesAsync(cancellationToken);
		return true;
	}
}
