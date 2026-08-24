namespace Basket.Data.Outbox;
public class OutboxMessage : Entity<Guid>
{
    public Guid TenantId { get; set; }
    public string Type { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTime OccuredOn { get; set; } = default!;
    public DateTime? ProcessedOn { get; set; } = default!;
}
