namespace Shared.DDD;

public abstract class AuditedEntity<T> : Entity<T>, IAuditedObject where T : struct
{
  public DateTimeOffset CreatedAt { get; set; }
  public Guid? CreatedBy { get; set; }
  public DateTimeOffset? LastModified { get; set; }
  public Guid? LastModifiedBy { get; set; }
}

public abstract class FullAuditedEntity<T> : AuditedEntity<T>, IFullAuditedObject where T : struct
{
  public bool IsDeleted { get; set; }
  public DateTimeOffset? DeletedAt { get; set; }
  public Guid? DeletedBy { get; set; }
}
