namespace Accounts.Accounts.Features.DeleteAddress;

public sealed class DeleteAddressEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
		=> app.MapDelete("/account/me/addresses/{addressId:guid}", Handle)
			.RequireAuthorization()
			.WithTags("Account")
			.WithName("DeleteMyAddress");

	private static async Task<IResult> Handle(
		Guid addressId,
		ISender sender,
		ICurrentUser currentUser,
		CancellationToken cancellationToken)
	{
		if (currentUser.Id is not Guid customerId)
		{
			return Results.Unauthorized();
		}

		bool deleted = await sender.Send(new DeleteAddressCommand(customerId, addressId), cancellationToken);
		return deleted ? Results.NoContent() : Results.NotFound();
	}
}
