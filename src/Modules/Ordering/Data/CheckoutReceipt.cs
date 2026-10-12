namespace Ordering.Data;

public sealed class CheckoutReceipt
{
  public Guid TenantId { get; set; }
  public Guid EventId { get; set; }
  public Guid OrderId { get; set; }
}
