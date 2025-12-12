
namespace Catalog.Products.Features.GetProductById;

public record GetProductByIdQuery(Guid Id)
	: IQuery<GetProductByIdResult>;

public record GetProductByIdResult(
	ProductDto Product
);

public class GetProductsByIdHandler(CatalogDbContext catalogDbContext)
	: IQueryHandler<GetProductByIdQuery, GetProductByIdResult>
{
	public async Task<GetProductByIdResult> Handle(GetProductByIdQuery comamnd, CancellationToken cancellationToken)
	{
		Product? product = await catalogDbContext.Products
			.AsNoTracking()
			.SingleOrDefaultAsync(x => x.Id == comamnd.Id, cancellationToken);

		ProductDto productsDto = product.Adapt<ProductDto>();

		return new GetProductByIdResult(productsDto);
	}

}
