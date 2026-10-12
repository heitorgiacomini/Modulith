using System.Text.Json;
using Shared.Messaging.Events;
using Shared.Messaging.Outbox;
using Xunit;

namespace SharedTests;

public sealed class IntegrationEventTests
{
  [Fact]
  public void Event_identity_and_timestamp_survive_serialization_and_repeated_reads()
  {
    ProductPriceChangedIntegrationEvent original = new() { TenantId = Guid.NewGuid() };
    Guid id = original.EventId;
    DateTime timestamp = original.OccurredOn;
    var replay = JsonSerializer.Deserialize<ProductPriceChangedIntegrationEvent>(JsonSerializer.Serialize(original))!;
    Assert.Equal(id, original.EventId);
    Assert.Equal(id, replay.EventId);
    Assert.Equal(timestamp, replay.OccurredOn);
  }

  [Fact]
  public void Legacy_outbox_rows_use_the_persisted_row_id_as_message_id()
  {
    IntegrationOutboxMessage row = new()
    {
      Id = Guid.NewGuid(), TenantId = Guid.NewGuid(),
      Type = typeof(BasketCheckoutIntegrationEvent).AssemblyQualifiedName!, Content = "{}"
    };
    Assert.Equal(row.Id, row.Deserialize().EventId);
    Assert.Equal(row.Id, row.Deserialize().EventId);
    Assert.Equal(row.TenantId, row.Deserialize().TenantId);
  }

  [Fact]
  public void Outbox_rejects_unknown_contracts_and_mismatched_tenants()
  {
    var row = IntegrationOutboxMessage.From(new ProductPriceChangedIntegrationEvent { TenantId = Guid.NewGuid() });
    row.TenantId = Guid.NewGuid();
    Assert.Throws<InvalidOperationException>(() => row.Deserialize());
    row.Type = typeof(string).FullName!;
    Assert.Throws<InvalidOperationException>(() => row.Deserialize());
  }
}
