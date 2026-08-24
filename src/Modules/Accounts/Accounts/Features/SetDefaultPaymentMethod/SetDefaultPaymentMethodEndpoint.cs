namespace Accounts.Accounts.Features.SetDefaultPaymentMethod;

public sealed class SetDefaultPaymentMethodEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
		=> app.MapPut("/account/me/payment-methods/{paymentMethodId:guid}/default", Handle)
			.RequireAuthorization()
			.WithTags("Account")
			.WithName("SetDefaultMyPaymentMethod");

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

		SetDefaultPaymentMethodResult result = await sender.Send(
			new SetDefaultPaymentMethodCommand(customerId, paymentMethodId), cancellationToken);
		return result.PaymentMethod is null ? Results.NotFound() : Results.Ok(result.PaymentMethod);
	}
}
