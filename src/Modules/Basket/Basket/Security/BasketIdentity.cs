using System.Security.Claims;

namespace Basket.Basket.Security;

internal static class BasketIdentity
{
  public static string? GetUserName(ClaimsPrincipal user) =>
    user.FindFirstValue("preferred_username") ?? user.Identity?.Name;

  public static Guid? GetCustomerId(ClaimsPrincipal user)
  {
    string? subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    return Guid.TryParse(subject, out Guid customerId) ? customerId : null;
  }
}
