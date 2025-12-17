using Shared.DDD;

namespace Basket.Basket.Models;

public class ShoppingCart : Aggregate<Guid>
{
    public String UserName { get; private set; } = default!;

    private readonly List<ShoppingCartItem> _items = [];
    public IReadOnlyList<ShoppingCartItem> Items => this._items.AsReadOnly();
    public Decimal TotalPrice => this.Items.Sum(x => x.Price * x.Quantity);

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
            ShoppingCartItem newItem = new ShoppingCartItem(this.Id, productId, quantity, color, price, productName)
            {
                Id = Guid.NewGuid()
            };
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
