namespace Shared.DDD
{

	public interface IEntity<T> : IEntity where T : struct
	{
		T Id { get; set; }
	}
	public interface IEntity
	{
		DateTime? CreatedAt { get; set; }
		string? CreatedBy { get; set; }
		DateTime? LastModified { get; set; }
		string? LastModifiedBy { get; set; }

	}
}
