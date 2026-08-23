namespace Shared.DDD;

public interface ICreationAuditedObject
{
  DateTimeOffset CreatedAt { get; set; }
  Guid? CreatedBy { get; set; }
}

public interface IModificationAuditedObject
{
  DateTimeOffset? LastModified { get; set; }
  Guid? LastModifiedBy { get; set; }
}

public interface IAuditedObject : ICreationAuditedObject, IModificationAuditedObject
{
}

public interface ISoftDelete
{
  bool IsDeleted { get; set; }
}

public interface IDeletionAuditedObject : ISoftDelete
{
  DateTimeOffset? DeletedAt { get; set; }
  Guid? DeletedBy { get; set; }
}

public interface IFullAuditedObject : IAuditedObject, IDeletionAuditedObject
{
}
