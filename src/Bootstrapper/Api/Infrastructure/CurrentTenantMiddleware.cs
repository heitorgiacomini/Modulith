using System.Security.Claims;
using Shared.Data.MultiTenancy;

namespace Api.Infrastructure;

public sealed class CurrentTenantMiddleware(RequestDelegate next)
{
  public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant)
  {
    string? organizationClaim = context.User.FindFirstValue("organization");
    if (context.User.Identity?.IsAuthenticated == true &&
      KeycloakOrganizationClaimParser.TryParse(organizationClaim, out KeycloakOrganization? organization))
    {
      using (currentTenant.Change(organization!.Id, organization.Alias))
      {
        await next(context);
      }

      return;
    }

    await next(context);
  }
}
