using Api.Infrastructure;
using Xunit;

namespace SharedTests;

public sealed class KeycloakOrganizationClaimParserTests
{
  [Fact]
  public void Parses_one_organization_with_id_alias_and_roles()
  {
    const string claim = """
      {"acme":{"id":"11111111-1111-1111-1111-111111111111","groups":["/Admins"],"realm_access":{"roles":["admin"]}}}
      """;

    bool parsed = KeycloakOrganizationClaimParser.TryParse(claim, out KeycloakOrganization? organization);

    Assert.True(parsed);
    Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), organization!.Id);
    Assert.Equal("acme", organization.Alias);
    Assert.Contains("admin", organization.Roles);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("not-json")]
  [InlineData("{}")]
  [InlineData("{\"acme\":{}}")]
  [InlineData("{\"acme\":{\"id\":\"not-a-guid\"}}")]
  [InlineData("{\"acme\":{\"id\":\"00000000-0000-0000-0000-000000000000\"}}")]
  [InlineData("{\"acme\":{\"id\":\"11111111-1111-1111-1111-111111111111\"},\"contoso\":{\"id\":\"22222222-2222-2222-2222-222222222222\"}}")]
  public void Rejects_missing_invalid_or_multiple_organizations(string? claim)
  {
    Assert.False(KeycloakOrganizationClaimParser.TryParse(claim, out _));
  }
}
