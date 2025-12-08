using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.DDD;

namespace Shared.Data.Interceptors;

public class DispatchDomainEventsInterceptor(IMediator mediator) : SaveChangesInterceptor
{
	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
	{
		DispatchDomainEvents(eventData.Context).GetAwaiter().GetResult();
		return base.SavingChanges(eventData, result);
	}

	public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
	{
		DispatchDomainEvents(eventData.Context).GetAwaiter().GetResult();
		return await base.SavingChangesAsync(eventData, result, cancellationToken);
	}

	private async Task DispatchDomainEvents(DbContext? context)
	{
		if (context is null)
		{
			return;
		}

		IEnumerable<IAggregate> aggregates = context.ChangeTracker
			.Entries<IAggregate>()
			.Where(e => e.Entity.DomainEvents.Any())
			.Select(e => e.Entity);

		List<IDomainEvent> domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

		aggregates.ToList().ForEach(a => a.ClearDomainEvents());

		foreach (IDomainEvent domainEvent in domainEvents)
		{
			await mediator.Publish(domainEvent);
		}
	}
}
