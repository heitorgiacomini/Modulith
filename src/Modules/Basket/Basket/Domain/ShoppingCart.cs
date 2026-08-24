namespace Basket.Basket.Domain;

public class ShoppingCart : FullAuditedAggregate<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public String UserName { get; private set; } = default!;

    private readonly List<ShoppingCartItem> _items = [];
    public IReadOnlyList<ShoppingCartItem> Items => this._items.AsReadOnly();
    public Decimal TotalPrice => this.Items.Sum(x => x.Price * x.Quantity);

    private ShoppingCart()
    {
    }

    public static ShoppingCart Create(Guid id, String userName)
    {
        ArgumentException.ThrowIfNullOrEmpty(userName);

        ShoppingCart shoppingCart = new ShoppingCart
        {
            Id = id,
            UserName = userName
        };

        return shoppingCart;
    }

    internal static ShoppingCart Restore(Guid id, String userName, IEnumerable<ShoppingCartItem> items)
    {
        ShoppingCart shoppingCart = Create(id, userName);
        shoppingCart._items.AddRange(items);
        return shoppingCart;
    }

    public void AddItem(Guid productId, Int32 quantity, String color, Decimal price, String productName)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        ShoppingCartItem? existingItem = this.Items.FirstOrDefault(x => x.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            ShoppingCartItem newItem = ShoppingCartItem.Create(this.Id, productId, quantity, color, price, productName);
            this._items.Add(newItem);
        }
    }

    public void RemoveItem(Guid productId)
    {
        ShoppingCartItem? existingItem = this.Items.FirstOrDefault(x => x.ProductId == productId);

        if (existingItem != null)
        {
            _ = this._items.Remove(existingItem);
        }
    }
}
