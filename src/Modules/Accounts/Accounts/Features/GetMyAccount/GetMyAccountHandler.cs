namespace Accounts.Accounts.Features.GetMyAccount;

public record GetMyAccountQuery(Guid CustomerId) : IQuery<AccountDto>;

internal sealed class GetMyAccountHandler(AccountsDbContext dbContext)
	: IQueryHandler<GetMyAccountQuery, AccountDto>
{
	public async Task<AccountDto> Handle(GetMyAccountQuery query, CancellationToken cancellationToken)
	{
		CustomerAccount? account = await dbContext.CustomerAccounts
			.AsNoTracking()
			.Include(item => item.Addresses)
			.Include(item => item.PaymentMethods)
			.SingleOrDefaultAsync(item => item.UserId == query.CustomerId, cancellationToken);

		return account is null ? AccountDtoDefaults.Empty() : AccountMapper.ToDto(account);
	}
}
