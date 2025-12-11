namespace Shared.DDD;

public abstract class Aggregate<TId> : Entity<TId>, IAggregate<TId> where TId : struct
{
	private readonly List<IDomainEvent> _domainEvents = [];

	public IReadOnlyList<IDomainEvent> DomainEvents => this._domainEvents.AsReadOnly();

	public void AddDomainEvent(IDomainEvent domainEvent)
	{
		this._domainEvents.Add(domainEvent);
	}
	public IDomainEvent[] ClearDomainEvents()
	{
		IDomainEvent[] dequeuedEvents = this._domainEvents.ToArray();
		this._domainEvents.Clear();
		return dequeuedEvents;
	}
}
