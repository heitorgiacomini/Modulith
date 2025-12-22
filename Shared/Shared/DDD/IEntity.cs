namespace Shared.DDD;

//IAuditableEntity
public interface IEntity<T> : IEntity where T : struct
{
	T Id { get; set; }
}
public interface IEntity
{
	DateTime? CreatedAt { get; set; }
	String? CreatedBy { get; set; }
	DateTime? LastModified { get; set; }
	String? LastModifiedBy { get; set; }

}
