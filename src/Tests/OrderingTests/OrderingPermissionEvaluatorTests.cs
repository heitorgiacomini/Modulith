using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Ordering.Orders.Authorization;
using Shared.Data.Auditing;
using Xunit;

namespace OrderingTests;

public sealed class OrderingPermissionEvaluatorTests
{
    [Fact]
    public void Evaluate_ReturnsCustomerAndOwnScope_FromValidRptClaims()
    {
        Guid customerId = Guid.NewGuid();
        ClaimsPrincipal user = CreateUser(
            customerId,
            "ordering-api",
            """
            {"permissions":[{"rsname":"Orders","scopes":["orders:read-own"]}]}
            """);

        OrderingPermission? permission = CreateEvaluator(user).Evaluate();

        Assert.NotNull(permission);
        Assert.Equal(customerId, permission.CustomerId);
        Assert.True(permission.HasScope("orders:read-own"));
        Assert.False(permission.HasScope("orders:read-all"));
    }

    [Fact]
    public void Evaluate_ReturnsAllScope_ForAdminRptClaims()
    {
        ClaimsPrincipal user = CreateUser(
            Guid.NewGuid(),
            "ordering-api",
            """
            {"permissions":[{"resource_name":"Orders","scopes":["orders:read-all","orders:delete-all"]}]}
            """);

        OrderingPermission? permission = CreateEvaluator(user).Evaluate();

        Assert.NotNull(permission);
        Assert.True(permission.HasScope("orders:read-all"));
        Assert.True(permission.HasScope("orders:delete-all"));
    }

    [Theory]
    [InlineData("other-api", "11111111-1111-1111-1111-111111111111")]
    [InlineData("ordering-api", "not-a-guid")]
    public void Evaluate_RejectsInvalidAudienceOrSubject(string audience, string subject)
    {
        ClaimsPrincipal user = CreateUser(
            subject,
            audience,
            """
            {"permissions":[{"rsname":"Orders","scopes":["orders:read-own"]}]}
            """);

        Assert.Null(CreateEvaluator(user).Evaluate());
    }

    [Fact]
    public void Evaluate_IgnoresScopesForOtherResources()
    {
        ClaimsPrincipal user = CreateUser(
            Guid.NewGuid(),
            "ordering-api",
            """
            {"permissions":[{"rsname":"Accounts","scopes":["orders:read-all"]}]}
            """);

        Assert.Null(CreateEvaluator(user).Evaluate());
    }

    private static ClaimsPrincipal CreateUser(Guid subject, string audience, string authorization)
        => CreateUser(subject.ToString(), audience, authorization);

    private static ClaimsPrincipal CreateUser(string subject, string audience, string authorization)
        => new(new ClaimsIdentity(
        [
            new Claim("sub", subject),
            new Claim("aud", audience),
            new Claim("authorization", authorization)
        ], "Test"));

    private static OrderingPermissionEvaluator CreateEvaluator(ClaimsPrincipal user)
    {
        DefaultHttpContext context = new() { User = user };
        HttpContextAccessor accessor = new() { HttpContext = context };
        return new OrderingPermissionEvaluator(new TestCurrentUser(user), accessor);
    }

    private sealed class TestCurrentUser(ClaimsPrincipal user) : ICurrentUser
    {
        public Guid? Id => Guid.TryParse(user.FindFirstValue("sub"), out Guid id) ? id : null;
        public string? UserName => null;
        public bool IsAuthenticated => user.Identity?.IsAuthenticated == true;
        public string? TraceId => null;
    }
}
