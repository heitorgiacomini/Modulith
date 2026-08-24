namespace Accounts.Accounts.Features.DeletePaymentMethod;

public sealed class DeletePaymentMethodEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
		=> app.MapDelete("/account/me/payment-methods/{paymentMethodId:guid}", Handle)
			.RequireAuthorization()
			.WithTags("Account")
			.WithName("DeleteMyPaymentMethod");

	private static async Task<IResult> Handle(
		Guid paymentMethodId,
		ISender sender,
		ICurrentUser currentUser,
		CancellationToken cancellationToken)
	{
		if (currentUser.Id is not Guid customerId)
		{
			return Results.Unauthorized();
		}

		bool deleted = await sender.Send(
			new DeletePaymentMethodCommand(customerId, paymentMethodId), cancellationToken);
		return deleted ? Results.NoContent() : Results.NotFound();
	}
}
