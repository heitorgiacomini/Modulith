namespace Shared.DDD;

public interface IEntity<T> : IEntity where T : struct
{
	T Id { get; set; }
}
public interface IEntity
{
}
