namespace Accounts.Accounts.Features.UpdatePreferences;

public sealed class UpdatePreferencesEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
		=> app.MapPut("/account/me/preferences", Handle)
			.RequireAuthorization()
			.WithTags("Account")
			.WithName("UpdateMyPreferences");

	private static async Task<IResult> Handle(
		PreferencesDto preferences,
		ISender sender,
		ICurrentUser currentUser,
		CancellationToken cancellationToken)
	{
		if (currentUser.Id is not Guid customerId)
		{
			return Results.Unauthorized();
		}

		return Results.Ok(await sender.Send(new UpdatePreferencesCommand(customerId, preferences), cancellationToken));
	}
}
