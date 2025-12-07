namespace Shared.DDD
{
	public interface IAggregate<T> : IAggregate, IEntity<T> where T : struct
	{

	}
	public interface IAggregate : IEntity
	{
		IReadOnlyList<IDomainEvent> DomainEvents { get; }
		IDomainEvent[] ClearDomainEvents();
	}
}
