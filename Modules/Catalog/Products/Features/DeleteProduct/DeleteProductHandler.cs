

namespace Catalog.Products.Features.DeleteProduct;

public record DeleteProductCommand(Guid ProductId)
	: ICommand<DeleteProductResult>;

public record DeleteProductResult(Boolean IsSuccess);

public class DeleteProductHandler(CatalogDbContext catalogDbContext)
	: ICommandHandler<DeleteProductCommand, DeleteProductResult>
{
	public async Task<DeleteProductResult> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
	{
		Product? product = await catalogDbContext.Products
			.FindAsync([command.ProductId], cancellationToken: cancellationToken);

		if (product == null)
		{
			return new DeleteProductResult(false);
		}

		_ = catalogDbContext.Products.Remove(product);
		_ = await catalogDbContext.SaveChangesAsync(cancellationToken);
		return new DeleteProductResult(true);
	}
}
