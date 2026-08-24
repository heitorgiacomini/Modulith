namespace Accounts.Accounts.Features.AddPaymentMethod;

public sealed class AddPaymentMethodEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
		=> app.MapPost("/account/me/payment-methods", Handle)
			.RequireAuthorization()
			.WithTags("Account")
			.WithName("AddMyPaymentMethod");

	private static async Task<IResult> Handle(
		SavePaymentMethodDto paymentMethod,
		ISender sender,
		ICurrentUser currentUser,
		CancellationToken cancellationToken)
	{
		if (currentUser.Id is not Guid customerId)
		{
			return Results.Unauthorized();
		}

		SavedPaymentMethodDto result = await sender.Send(
			new AddPaymentMethodCommand(customerId, paymentMethod), cancellationToken);
		return Results.Created($"/account/me/payment-methods/{result.Id}", result);
	}
}
