using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace Ordering.Orders.Authorization;

internal static class OrderingAuthorization
{
    public const string Audience = "ordering-api";
    public const string Resource = "Orders";

    public const string CreatePolicy = "OrdersCreate";
    public const string ReadPolicy = "OrdersRead";
    public const string DeletePolicy = "OrdersDelete";

    public const string CreateOwnScope = "orders:create-own";
    public const string ReadOwnScope = "orders:read-own";
    public const string ReadAllScope = "orders:read-all";
    public const string DeleteOwnScope = "orders:delete-own";
    public const string DeleteAllScope = "orders:delete-all";
}

public sealed record OrderingPermission(Guid CustomerId, IReadOnlySet<string> Scopes)
{
    public bool HasScope(string scope) => Scopes.Contains(scope);
}

public interface IOrderingPermissionEvaluator
{
    OrderingPermission? Evaluate();
}

public sealed class OrderingPermissionEvaluator(
    ICurrentUser currentUser,
    ICurrentTenant currentTenant,
    IHttpContextAccessor httpContextAccessor) : IOrderingPermissionEvaluator
{
    public OrderingPermission? Evaluate()
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        if (user is null ||
            !currentUser.IsAuthenticated ||
            !currentTenant.IsAvailable ||
            currentUser.Id is not Guid customerId ||
            !TryGetOrganizationRoles(user, currentTenant.Id!.Value, out IReadOnlySet<string>? organizationRoles) ||
            !HasAudience(user, OrderingAuthorization.Audience) ||
            user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        Claim? authorizationClaim = user.FindFirst("authorization");
        if (authorizationClaim is null)
        {
            return null;
        }

        try
        {
            using JsonDocument authorization = JsonDocument.Parse(authorizationClaim.Value);
            if (!authorization.RootElement.TryGetProperty("permissions", out JsonElement permissions))
            {
                return null;
            }

            HashSet<string> scopes = [];
            foreach (JsonElement permission in permissions.EnumerateArray())
            {
                string? resourceName = permission.TryGetProperty("rsname", out JsonElement rsname)
                    ? rsname.GetString()
                    : permission.TryGetProperty("resource_name", out JsonElement resource)
                        ? resource.GetString()
                        : null;

                if (!string.Equals(resourceName, OrderingAuthorization.Resource, StringComparison.Ordinal) ||
                    !permission.TryGetProperty("scopes", out JsonElement grantedScopes))
                {
                    continue;
                }

                foreach (JsonElement scope in grantedScopes.EnumerateArray())
                {
                    if (scope.GetString() is { } value)
                    {
                        bool isTenantAdmin = organizationRoles.Contains("admin");
                        bool isTenantMember = isTenantAdmin || organizationRoles.Contains("customer");
                        if ((value.EndsWith("-all", StringComparison.Ordinal) && isTenantAdmin) ||
                            (value.EndsWith("-own", StringComparison.Ordinal) && isTenantMember))
                        {
                            scopes.Add(value);
                        }
                    }
                }
            }

            return scopes.Count == 0 ? null : new OrderingPermission(customerId, scopes);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool HasAudience(ClaimsPrincipal user, string audience)
        => user.FindAll("aud").Any(claim =>
            string.Equals(claim.Value, audience, StringComparison.Ordinal) ||
            TryReadAudienceArray(claim.Value, audience));

    private static bool TryGetOrganizationRoles(
        ClaimsPrincipal user,
        Guid tenantId,
        out IReadOnlySet<string> roles)
    {
        roles = new HashSet<string>(StringComparer.Ordinal);
        string? claim = user.FindFirstValue("organization");
        if (string.IsNullOrWhiteSpace(claim))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(claim);
            JsonProperty[] organizations = document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().ToArray()
                : [];
            if (organizations.Length != 1 ||
                organizations[0].Value.ValueKind != JsonValueKind.Object ||
                !organizations[0].Value.TryGetProperty("id", out JsonElement idValue) ||
                !Guid.TryParse(idValue.GetString(), out Guid claimTenantId) ||
                claimTenantId != tenantId)
            {
                return false;
            }

            HashSet<string> effectiveRoles = new(StringComparer.Ordinal);
            if (organizations[0].Value.TryGetProperty("realm_access", out JsonElement realmAccess) &&
                realmAccess.TryGetProperty("roles", out JsonElement roleValues) &&
                roleValues.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement role in roleValues.EnumerateArray())
                {
                    if (role.GetString() is { } roleName)
                    {
                        effectiveRoles.Add(roleName);
                    }
                }
            }

            roles = effectiveRoles;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadAudienceArray(string value, string audience)
    {
        try
        {
            using JsonDocument audiences = JsonDocument.Parse(value);
            return audiences.RootElement.ValueKind == JsonValueKind.Array &&
                audiences.RootElement.EnumerateArray().Any(item =>
                    string.Equals(item.GetString(), audience, StringComparison.Ordinal));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

internal sealed class OrderingScopeRequirement(params string[] scopes) : IAuthorizationRequirement
{
    public IReadOnlySet<string> Scopes { get; } = scopes.ToHashSet(StringComparer.Ordinal);
}

internal sealed class OrderingScopeAuthorizationHandler(IOrderingPermissionEvaluator evaluator)
    : AuthorizationHandler<OrderingScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderingScopeRequirement requirement)
    {
        OrderingPermission? permission = evaluator.Evaluate();
        if (permission is not null && permission.Scopes.Overlaps(requirement.Scopes))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
