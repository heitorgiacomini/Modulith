namespace Ordering.Orders.Domain;
public class OrderItem : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    private OrderItem()
    {
    }

    internal static OrderItem Create(Guid orderId, Guid productId, int quantity, decimal price)
    {
        return new OrderItem
        {
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            Price = price
        };
    }

    public Guid OrderId { get; private set; } = default!;
    public Guid ProductId { get; private set; } = default!;
    public int Quantity { get; internal set; } = default!;
    public decimal Price { get; private set; } = default!;
}
