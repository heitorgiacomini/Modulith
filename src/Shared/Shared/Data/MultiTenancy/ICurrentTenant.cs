namespace Shared.Data.MultiTenancy;

public interface ICurrentTenant
{
  Guid? Id { get; }
  string? Name { get; }
  bool IsAvailable { get; }
  IDisposable Change(Guid? id, string? name = null);
}

public static class TenantAuthorizationPolicies
{
  public const string Member = "TenantMember";
  public const string Admin = "TenantAdmin";
}
