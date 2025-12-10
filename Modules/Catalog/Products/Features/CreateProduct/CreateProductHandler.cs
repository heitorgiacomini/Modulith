namespace Catalog.Products.Features.CreateProduct;

public record CreateProductCommand
	(ProductDto Product)
	: ICommand<CreateProductResult>;
public record CreateProductResult(Guid Id);


public class CreateProductHandler(CatalogDbContext dbContext)
	: ICommandHandler<CreateProductCommand, CreateProductResult>
{

	public async Task<CreateProductResult> Handle(CreateProductCommand command,
		CancellationToken cancellationToken)
	{
		Product product = CreateNewProduct(command.Product);
		_ = dbContext.Products.Add(product);
		_ = await dbContext.SaveChangesAsync(cancellationToken);
		return new CreateProductResult(product.Id);

	}

	private static Product CreateNewProduct(ProductDto productDto)
	{
		Product product = Product.Create(
			Guid.NewGuid(),
			productDto.Name,
			productDto.Category,
			productDto.Description,
			productDto.ImageFile,
			productDto.Price);

		return product;
	}
}
