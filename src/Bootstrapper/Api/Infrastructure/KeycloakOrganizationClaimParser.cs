using System.Text.Json;

namespace Api.Infrastructure;

public sealed record KeycloakOrganization(Guid Id, string Alias, IReadOnlySet<string> Roles);

public static class KeycloakOrganizationClaimParser
{
  public static bool TryParse(string? value, out KeycloakOrganization? organization)
  {
    organization = null;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    try
    {
      using JsonDocument document = JsonDocument.Parse(value);
      if (document.RootElement.ValueKind != JsonValueKind.Object)
      {
        return false;
      }

      JsonProperty[] organizations = document.RootElement.EnumerateObject().ToArray();
      if (organizations.Length != 1 ||
        organizations[0].Value.ValueKind != JsonValueKind.Object ||
        !organizations[0].Value.TryGetProperty("id", out JsonElement idElement) ||
        !Guid.TryParse(idElement.GetString(), out Guid id) ||
        id == Guid.Empty)
      {
        return false;
      }

      HashSet<string> roles = [];
      if (organizations[0].Value.TryGetProperty("realm_access", out JsonElement realmAccess) &&
        realmAccess.TryGetProperty("roles", out JsonElement roleValues) &&
        roleValues.ValueKind == JsonValueKind.Array)
      {
        foreach (JsonElement role in roleValues.EnumerateArray())
        {
          if (role.GetString() is { } roleName)
          {
            roles.Add(roleName);
          }
        }
      }

      organization = new KeycloakOrganization(id, organizations[0].Name, roles);
      return true;
    }
    catch (JsonException)
    {
      return false;
    }
  }
}
