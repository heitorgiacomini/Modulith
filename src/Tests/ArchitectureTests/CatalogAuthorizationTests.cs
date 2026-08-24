using System.Security.Claims;
using System.Text.Json;
using Catalog.Products.Authorization;
using Microsoft.AspNetCore.Authorization;
using Shared.Data.MultiTenancy;
using Xunit;

namespace ArchitectureTests;

public sealed class CatalogAuthorizationTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task AdminRequirement_SucceedsForAdminInActiveTenant()
    {
        CurrentTenant tenant = new();
        using IDisposable scope = tenant.Change(TenantId);
        CatalogAdminAuthorizationHandler handler = new(tenant);
        CatalogAdminRequirement requirement = new();
        AuthorizationHandlerContext context = new(
            [requirement],
            CreatePrincipal(TenantId, "admin"),
            resource: null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Theory]
    [InlineData("customer")]
    [InlineData("admin")]
    public async Task AdminRequirement_FailsForWrongRoleOrTenant(string role)
    {
        CurrentTenant tenant = new();
        using IDisposable scope = tenant.Change(TenantId);
        CatalogAdminAuthorizationHandler handler = new(tenant);
        CatalogAdminRequirement requirement = new();
        Guid claimTenant = role == "admin" ? Guid.NewGuid() : TenantId;
        AuthorizationHandlerContext context = new(
            [requirement],
            CreatePrincipal(claimTenant, role),
            resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static ClaimsPrincipal CreatePrincipal(Guid tenantId, string role)
    {
        string organizationJson = JsonSerializer.Serialize(new
        {
            tenant = new
            {
                id = tenantId,
                realm_access = new { roles = new[] { role } }
            }
        });
        Claim organization = new("organization", organizationJson);
        return new ClaimsPrincipal(new ClaimsIdentity([organization], "test"));
    }
}
