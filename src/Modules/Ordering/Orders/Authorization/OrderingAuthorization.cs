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
    OrderingPermission? Evaluate(ClaimsPrincipal user);
}

public sealed class OrderingPermissionEvaluator : IOrderingPermissionEvaluator
{
    public OrderingPermission? Evaluate(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true ||
            !HasAudience(user, OrderingAuthorization.Audience) ||
            !Guid.TryParse(user.FindFirstValue("sub"), out Guid customerId))
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
                        scopes.Add(value);
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
        OrderingPermission? permission = evaluator.Evaluate(context.User);
        if (permission is not null && permission.Scopes.Overlaps(requirement.Scopes))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}