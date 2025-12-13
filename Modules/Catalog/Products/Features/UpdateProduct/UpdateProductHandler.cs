namespace Catalog.Products.Features.UpdateProduct;

public record UpdateProductCommand
				(ProductDto Product)
				: ICommand<UpdateProductResult>;

public record UpdateProductResult(Boolean IsSuccess);
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
	public UpdateProductCommandValidator()
	{
		_ = this.RuleFor(x => x.Product.Id).NotEmpty().WithMessage("Id is required");
		_ = this.RuleFor(x => x.Product.Name).NotEmpty().WithMessage("Name is required");
		_ = this.RuleFor(x => x.Product.Price).GreaterThan(0).WithMessage("Price must be greater than 0");
	}
}

public class UpdateProductHandler(CatalogDbContext dbContext)
	: ICommandHandler<UpdateProductCommand, UpdateProductResult>
{
	public async Task<UpdateProductResult> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
	{
		Product? product = await dbContext.Products
			.FindAsync([command.Product.Id], cancellationToken: cancellationToken);

		if (product == null)
		{
			throw new ProductNotFoundException(command.Product.Id);
		}

		UpdateProductWithNewValue(product, command.Product);
		_ = dbContext.Products.Update(product);
		_ = await dbContext.SaveChangesAsync(cancellationToken);
		return new UpdateProductResult(true);
	}

	private static void UpdateProductWithNewValue(Product product, ProductDto productDto)
	{
		product.Update(
			productDto.Name,
			productDto.Category,
			productDto.Description,
			productDto.ImageFile,
			productDto.Price);
	}
}
