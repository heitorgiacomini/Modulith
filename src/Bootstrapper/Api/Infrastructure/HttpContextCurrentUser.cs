using System.Diagnostics;
using System.Security.Claims;
using Shared.Data.Auditing;

namespace Api.Infrastructure;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
  : ICurrentUser
{
  private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

  public Guid? Id
  {
    get
    {
      string? subject = Principal?.FindFirstValue("sub");
      return Guid.TryParse(subject, out Guid userId) ? userId : null;
    }
  }

  public string? UserName =>
    Principal?.FindFirstValue("preferred_username") ?? Principal?.Identity?.Name;

  public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

  public string? TraceId =>
    Activity.Current?.Id ?? httpContextAccessor.HttpContext?.TraceIdentifier;
}
