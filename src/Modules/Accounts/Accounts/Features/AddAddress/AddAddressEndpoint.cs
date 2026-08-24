namespace Accounts.Accounts.Features.AddAddress;

public sealed class AddAddressEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
		=> app.MapPost("/account/me/addresses", Handle)
			.RequireAuthorization()
			.WithTags("Account")
			.WithName("AddMyAddress");

	private static async Task<IResult> Handle(
		SaveAddressDto address,
		ISender sender,
		ICurrentUser currentUser,
		CancellationToken cancellationToken)
	{
		if (currentUser.Id is not Guid customerId)
		{
			return Results.Unauthorized();
		}

		SavedAddressDto result = await sender.Send(new AddAddressCommand(customerId, address), cancellationToken);
		return Results.Created($"/account/me/addresses/{result.Id}", result);
	}
}
