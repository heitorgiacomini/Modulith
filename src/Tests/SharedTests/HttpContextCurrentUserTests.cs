using System.Security.Claims;
using Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace SharedTests;

public sealed class HttpContextCurrentUserTests
{
  [Fact]
  public void Id_uses_only_keycloak_subject_guid()
  {
    Guid expected = Guid.NewGuid();
    HttpContextCurrentUser currentUser = CreateCurrentUser(
      new Claim("sub", expected.ToString()),
      new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

    Assert.Equal(expected, currentUser.Id);
    Assert.True(currentUser.IsAuthenticated);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("not-a-guid")]
  [InlineData("username")]
  public void Id_is_null_when_subject_is_not_a_guid(string? subject)
  {
    List<Claim> claims = [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())];
    if (subject is not null)
    {
      claims.Add(new Claim("sub", subject));
    }

    HttpContextCurrentUser currentUser = CreateCurrentUser([.. claims]);

    Assert.Null(currentUser.Id);
  }

  [Fact]
  public void UserName_prefers_keycloak_preferred_username()
  {
    HttpContextCurrentUser currentUser = CreateCurrentUser(
      new Claim("preferred_username", "keycloak-user"),
      new Claim(ClaimTypes.Name, "identity-name"));

    Assert.Equal("keycloak-user", currentUser.UserName);
  }

  [Fact]
  public void UserName_falls_back_to_identity_name()
  {
    HttpContextCurrentUser currentUser = CreateCurrentUser(
      new Claim(ClaimTypes.Name, "identity-name"));

    Assert.Equal("identity-name", currentUser.UserName);
  }

  [Fact]
  public void No_http_context_represents_background_operation()
  {
    HttpContextCurrentUser currentUser = new(new HttpContextAccessor());

    Assert.Null(currentUser.Id);
    Assert.Null(currentUser.UserName);
    Assert.False(currentUser.IsAuthenticated);
  }

  private static HttpContextCurrentUser CreateCurrentUser(params Claim[] claims)
  {
    DefaultHttpContext httpContext = new()
    {
      User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
    };
    HttpContextAccessor accessor = new() { HttpContext = httpContext };
    return new HttpContextCurrentUser(accessor);
  }
}
