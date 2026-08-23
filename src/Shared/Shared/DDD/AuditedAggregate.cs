namespace Shared.DDD;

public abstract class AuditedAggregate<TId> : Aggregate<TId>, IAuditedObject
  where TId : struct
{
  public DateTimeOffset CreatedAt { get; set; }
  public Guid? CreatedBy { get; set; }
  public DateTimeOffset? LastModified { get; set; }
  public Guid? LastModifiedBy { get; set; }
}

public abstract class FullAuditedAggregate<TId> : AuditedAggregate<TId>, IFullAuditedObject
  where TId : struct
{
  public bool IsDeleted { get; set; }
  public DateTimeOffset? DeletedAt { get; set; }
  public Guid? DeletedBy { get; set; }
}
