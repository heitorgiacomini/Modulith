using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Shared.Data.MultiTenancy;

namespace Catalog.Products.Authorization;

internal static class CatalogAuthorization
{
    public const string AdminPolicy = "TenantAdmin";
}

internal sealed class CatalogAdminRequirement : IAuthorizationRequirement;

internal sealed class CatalogAdminAuthorizationHandler(ICurrentTenant currentTenant)
    : AuthorizationHandler<CatalogAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CatalogAdminRequirement requirement)
    {
        if (currentTenant.Id is { } tenantId && IsTenantAdmin(context.User, tenantId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool IsTenantAdmin(ClaimsPrincipal user, Guid tenantId)
    {
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

            return organizations.Length == 1 &&
                organizations[0].Value.TryGetProperty("id", out JsonElement id) &&
                Guid.TryParse(id.GetString(), out Guid claimTenantId) &&
                claimTenantId == tenantId &&
                organizations[0].Value.TryGetProperty("realm_access", out JsonElement realmAccess) &&
                realmAccess.TryGetProperty("roles", out JsonElement roles) &&
                roles.ValueKind == JsonValueKind.Array &&
                roles.EnumerateArray().Any(role => role.GetString() == "admin");
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
