namespace Basket.Basket.Features.AddItemIntoBasket;

public record AddItemIntoBasketCommand(String UserName, ShoppingCartItemDto ShoppingCartItem)
    : ICommand<AddItemIntoBasketResult>;
public record AddItemIntoBasketResult(Guid Id);
public class AddItemIntoBasketCommandValidator : AbstractValidator<AddItemIntoBasketCommand>
{
  public AddItemIntoBasketCommandValidator()
  {
    _ = this.RuleFor(x => x.UserName).NotEmpty().WithMessage("UserName is required");
    _ = this.RuleFor(x => x.ShoppingCartItem.ProductId).NotEmpty().WithMessage("ProductId is required");
    _ = this.RuleFor(x => x.ShoppingCartItem.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");
  }
}

internal class AddItemIntoBasketHandler(IBasketRepository repository, ISender sender)
    : ICommandHandler<AddItemIntoBasketCommand, AddItemIntoBasketResult>
{
  public async Task<AddItemIntoBasketResult> Handle(AddItemIntoBasketCommand command, CancellationToken cancellationToken)
  {
    ShoppingCart shoppingCart = await repository.GetBasketAsync(command.UserName, false, cancellationToken);
    // Add shopping cart item into shopping cart
    GetProductByIdResult result = await sender.Send(new GetProductByIdQuery(command.ShoppingCartItem.ProductId), cancellationToken);


    shoppingCart.AddItem(
      command.ShoppingCartItem.ProductId,
      command.ShoppingCartItem.Quantity,
      command.ShoppingCartItem.Color,
      result.Product.Price,
      result.Product.Name
    );
    //command.ShoppingCartItem.Price,
    //        command.ShoppingCartItem.ProductName);

    _ = await repository.SaveChangesAsync(command.UserName, cancellationToken);

    return new AddItemIntoBasketResult(shoppingCart.Id);
  }
}
