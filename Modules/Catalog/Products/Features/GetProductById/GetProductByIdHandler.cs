
namespace Catalog.Products.Features.GetProductById;

public record GetProductsByIdQuery(Guid Id)
	: IQuery<GetProductsByIdResul>;

public record GetProductsByIdResul(
	ProductDto Products
);

public class GetProductsByIdHandler(CatalogDbContext catalogDbContext)
	: IQueryHandler<GetProductsByIdQuery, GetProductsByIdResul>
{
	public async Task<GetProductsByIdResul> Handle(GetProductsByIdQuery comamnd, CancellationToken cancellationToken)
	{
		Product? product = await catalogDbContext.Products
			.AsNoTracking()
			.SingleOrDefaultAsync(x => x.Id == comamnd.Id, cancellationToken);

		ProductDto productsDto = product.Adapt<ProductDto>();

		return new GetProductsByIdResul(productsDto);
	}

}
