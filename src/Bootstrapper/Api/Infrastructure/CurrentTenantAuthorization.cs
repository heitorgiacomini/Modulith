using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Shared.Data.MultiTenancy;

namespace Api.Infrastructure;

public sealed class CurrentTenantRequirement : IAuthorizationRequirement;

public sealed record OrganizationRoleRequirement(params string[] Roles) : IAuthorizationRequirement;

public sealed class CurrentTenantAuthorizationHandler(ICurrentTenant currentTenant)
  : AuthorizationHandler<CurrentTenantRequirement>
{
  protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    CurrentTenantRequirement requirement)
  {
    if (currentTenant.IsAvailable)
    {
      context.Succeed(requirement);
    }

    return Task.CompletedTask;
  }
}

public sealed class OrganizationRoleAuthorizationHandler(
  IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<OrganizationRoleRequirement>
{
  protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    OrganizationRoleRequirement requirement)
  {
    string? claim = httpContextAccessor.HttpContext?.User.FindFirst("organization")?.Value;
    if (KeycloakOrganizationClaimParser.TryParse(claim, out KeycloakOrganization? organization) &&
      organization!.Roles.Overlaps(requirement.Roles))
    {
      context.Succeed(requirement);
    }

    return Task.CompletedTask;
  }
}

public sealed class TenantAuthorizationMiddlewareResultHandler(
  ICurrentTenant currentTenant) : IAuthorizationMiddlewareResultHandler
{
  private readonly AuthorizationMiddlewareResultHandler fallback = new();

  public async Task HandleAsync(
    RequestDelegate next,
    HttpContext context,
    AuthorizationPolicy policy,
    PolicyAuthorizationResult authorizeResult)
  {
    if (authorizeResult.Forbidden && !currentTenant.IsAvailable &&
      context.User.Identity?.IsAuthenticated == true)
    {
      await Results.Problem(
        statusCode: StatusCodes.Status403Forbidden,
        title: "Tenant context required",
        detail: "The access token must contain exactly one organization with a valid UUID identifier.",
        instance: context.Request.Path).ExecuteAsync(context);
      return;
    }

    await fallback.HandleAsync(next, context, policy, authorizeResult);
  }
}
