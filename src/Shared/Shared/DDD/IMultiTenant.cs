namespace Shared.DDD;

public interface IMultiTenant
{
  Guid? TenantId { get; set; }
}
