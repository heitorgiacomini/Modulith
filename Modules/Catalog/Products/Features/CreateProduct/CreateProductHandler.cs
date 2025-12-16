namespace Catalog.Products.Features.CreateProduct;

public record CreateProductCommand
	(ProductDto Product)
	: ICommand<CreateProductResult>;
public record CreateProductResult(Guid Id);

public class CreateProductCommandValidator
	: AbstractValidator<CreateProductCommand>
{
	public CreateProductCommandValidator()
	{
		_ = this.RuleFor(x => x.Product.Name).NotEmpty().WithMessage("Name is required.");
		_ = this.RuleFor(x => x.Product.Category).NotEmpty().WithMessage("Category is required.");
		_ = this.RuleFor(x => x.Product.ImageFile).NotEmpty().WithMessage("ImageFile is required.");
		_ = this.RuleFor(x => x.Product.Price).GreaterThan(0).WithMessage("Price must be greater than 0.");
	}
}
public class CreateProductHandler(
	CatalogDbContext dbContext
	)
	: ICommandHandler<CreateProductCommand, CreateProductResult>
{

	public async Task<CreateProductResult> Handle(CreateProductCommand command,
		CancellationToken cancellationToken)
	{

		//logger.LogInformation("CreateProductCommandHandler.Handle called with {@Command}", command);

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
