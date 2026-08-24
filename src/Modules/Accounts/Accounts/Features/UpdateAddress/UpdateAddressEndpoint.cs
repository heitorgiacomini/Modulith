namespace Accounts.Accounts.Features.UpdateAddress;

public sealed class UpdateAddressEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
		=> app.MapPut("/account/me/addresses/{addressId:guid}", Handle)
			.RequireAuthorization()
			.WithTags("Account")
			.WithName("UpdateMyAddress");

	private static async Task<IResult> Handle(
		Guid addressId,
		SaveAddressDto address,
		ISender sender,
		ICurrentUser currentUser,
		CancellationToken cancellationToken)
	{
		if (currentUser.Id is not Guid customerId)
		{
			return Results.Unauthorized();
		}

		UpdateAddressResult result = await sender.Send(
			new UpdateAddressCommand(customerId, addressId, address), cancellationToken);
		return result.Address is null ? Results.NotFound() : Results.Ok(result.Address);
	}
}
