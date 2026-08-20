using Accounts.Accounts.Security;
using System.Security.Claims;

namespace Accounts.Accounts.Features;

public sealed class AccountEndpoints : ICarterModule
{
  public void AddRoutes(IEndpointRouteBuilder app)
  {
    RouteGroupBuilder account = app.MapGroup("/account/me")
      .RequireAuthorization()
      .WithTags("Account");

    account.MapGet("/", GetAccount).WithName("GetMyAccount");
    account.MapPut("/preferences", UpdatePreferences).WithName("UpdateMyPreferences");
    account.MapPost("/addresses", AddAddress).WithName("AddMyAddress");
    account.MapPut("/addresses/{addressId:guid}", UpdateAddress).WithName("UpdateMyAddress");
    account.MapDelete("/addresses/{addressId:guid}", DeleteAddress).WithName("DeleteMyAddress");
    account.MapPost("/payment-methods", AddPaymentMethod).WithName("AddMyPaymentMethod");
    account.MapPut("/payment-methods/{paymentMethodId:guid}/default", SetDefaultPaymentMethod).WithName("SetDefaultMyPaymentMethod");
    account.MapDelete("/payment-methods/{paymentMethodId:guid}", DeletePaymentMethod).WithName("DeleteMyPaymentMethod");
  }

  private static async Task<IResult> GetAccount(
    ISender sender,
    ClaimsPrincipal user,
    CancellationToken cancellationToken)
  {
    Guid? customerId = AccountIdentity.GetCustomerId(user);
    if (customerId is null)
    {
      return Results.Unauthorized();
    }

    AccountDto account = await sender.Send(new GetMyAccountQuery(customerId.Value), cancellationToken);
    return Results.Ok(account);
  }

  private static async Task<IResult> UpdatePreferences(
    PreferencesDto preferences,
    ISender sender,
    ClaimsPrincipal user,
    CancellationToken cancellationToken)
  {
    Guid? customerId = AccountIdentity.GetCustomerId(user);
    if (customerId is null)
    {
      return Results.Unauthorized();
    }

    AccountDto account = await sender.Send(
      new UpdatePreferencesCommand(customerId.Value, preferences),
      cancellationToken);
    return Results.Ok(account);
  }

  private static async Task<IResult> AddAddress(
    SaveAddressDto address,
    ISender sender,
    ClaimsPrincipal user,
    CancellationToken cancellationToken)
  {
    Guid? customerId = AccountIdentity.GetCustomerId(user);
    if (customerId is null)
    {
      return Results.Unauthorized();
    }

    SavedAddressDto result = await sender.Send(
      new AddAddressCommand(customerId.Value, address),
      cancellationToken);
    return Results.Created($"/account/me/addresses/{result.Id}", result);
  }

  private static async Task<IResult> UpdateAddress(
    Guid addressId,
    SaveAddressDto address,
    ISender sender,
    ClaimsPrincipal user,
    CancellationToken cancellationToken)
  {
    Guid? customerId = AccountIdentity.GetCustomerId(user);
    if (customerId is null)
    {
      return Results.Unauthorized();
    }

    UpdateAddressResult result = await sender.Send(
      new UpdateAddressCommand(customerId.Value, addressId, address),
      cancellationToken);
    return result.Address is null ? Results.NotFound() : Results.Ok(result.Address);
  }

  private static async Task<IResult> DeleteAddress(
    Guid addressId,
    ISender sender,
    ClaimsPrincipal user,
    CancellationToken cancellationToken)
  {
    Guid? customerId = AccountIdentity.GetCustomerId(user);
    if (customerId is null)
    {
      return Results.Unauthorized();
    }

    bool deleted = await sender.Send(
      new DeleteAddressCommand(customerId.Value, addressId),
      cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
  }

  private static async Task<IResult> AddPaymentMethod(
    SavePaymentMethodDto paymentMethod,
    ISender sender,
    ClaimsPrincipal user,
    CancellationToken cancellationToken)
  {
    Guid? customerId = AccountIdentity.GetCustomerId(user);
    if (customerId is null)
    {
      return Results.Unauthorized();
    }

    SavedPaymentMethodDto result = await sender.Send(
      new AddPaymentMethodCommand(customerId.Value, paymentMethod),
      cancellationToken);
    return Results.Created($"/account/me/payment-methods/{result.Id}", result);
  }

  private static async Task<IResult> DeletePaymentMethod(
    Guid paymentMethodId,
    ISender sender,
    ClaimsPrincipal user,
    CancellationToken cancellationToken)
  {
    Guid? customerId = AccountIdentity.GetCustomerId(user);
    if (customerId is null)
    {
      return Results.Unauthorized();
    }

    bool deleted = await sender.Send(
      new DeletePaymentMethodCommand(customerId.Value, paymentMethodId),
      cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
  }

  private static async Task<IResult> SetDefaultPaymentMethod(
    Guid paymentMethodId,
    ISender sender,
    ClaimsPrincipal user,
    CancellationToken cancellationToken)
  {
    Guid? customerId = AccountIdentity.GetCustomerId(user);
    if (customerId is null)
    {
      return Results.Unauthorized();
    }

    SetDefaultPaymentMethodResult result = await sender.Send(
      new SetDefaultPaymentMethodCommand(customerId.Value, paymentMethodId),
      cancellationToken);
    return result.PaymentMethod is null ? Results.NotFound() : Results.Ok(result.PaymentMethod);
  }
}
