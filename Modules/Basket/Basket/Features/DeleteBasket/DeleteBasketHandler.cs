namespace Basket.Basket.Features.DeleteBasket;

public record DeleteBasketCommand(String UserName)
    : ICommand<DeleteBasketResult>;
public record DeleteBasketResult(Boolean IsSuccess);

internal class DeleteBasketHandler(IBasketRepository repository)
    : ICommandHandler<DeleteBasketCommand, DeleteBasketResult>
{
    public async Task<DeleteBasketResult> Handle(DeleteBasketCommand command, CancellationToken cancellationToken)
    {
        _ = await repository.DeleteBasketAsync(command.UserName, cancellationToken);
        return new DeleteBasketResult(true);
    }
}
