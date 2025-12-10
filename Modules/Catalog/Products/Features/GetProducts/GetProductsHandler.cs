namespace Catalog.Products.Features.GetProducts;

public record GetProductsQuery()
	: IQuery<GetProductsResul>;

public record GetProductsResul(
	IEnumerable<ProductDto> Products
);

public class GetProductsHandler(CatalogDbContext catalogDbContext)
	: IQueryHandler<GetProductsQuery, GetProductsResul>
{
	public async Task<GetProductsResul> Handle(GetProductsQuery comamnd, CancellationToken cancellationToken)
	{
		List<Product> products = await catalogDbContext.Products
			.AsNoTracking()
			.OrderBy(p => p.Name)
			.ToListAsync(cancellationToken);

		List<ProductDto> productsDto = this.ProjectToProductDto(products);

		return new GetProductsResul(productsDto);
	}

	private List<ProductDto> ProjectToProductDto(List<Product> products)
	{
		throw new NotImplementedException();
	}
}
