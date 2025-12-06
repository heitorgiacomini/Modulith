

namespace Shared.DDD
{
    public abstract class Aggregate<TId> : Entity<TId>, IAggregate<TId> where TId : class
    {
        private readonly List<IDomainEvent> _domainEvents => [];
        public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();


        public IDomainEvent[] ClearDomainEvents()
        {
            throw new NotImplementedException();
        }
    }
}
