using System.Security.Claims;

namespace Accounts.Accounts.Security;

internal static class AccountIdentity
{
  public static Guid? GetCustomerId(ClaimsPrincipal user)
  {
    string? subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    return Guid.TryParse(subject, out Guid customerId) ? customerId : null;
  }
}
