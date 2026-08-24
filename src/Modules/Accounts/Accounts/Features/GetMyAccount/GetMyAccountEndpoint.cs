namespace Accounts.Accounts.Features.GetMyAccount;

public sealed class GetMyAccountEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
		=> app.MapGet("/account/me/", Handle)
			.RequireAuthorization()
			.WithTags("Account")
			.WithName("GetMyAccount");

	private static async Task<IResult> Handle(ISender sender, ICurrentUser currentUser, CancellationToken cancellationToken)
	{
		if (currentUser.Id is not Guid customerId)
		{
			return Results.Unauthorized();
		}

		return Results.Ok(await sender.Send(new GetMyAccountQuery(customerId), cancellationToken));
	}
}
