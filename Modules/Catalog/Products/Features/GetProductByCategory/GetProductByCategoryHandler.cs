namespace Catalog.Products.Features.GetProductByCategory;

public record GetProductsByCategoryQuery(String Category)
	: IQuery<GetProductsByCategoryResul>;

public record GetProductsByCategoryResul(
	IEnumerable<ProductDto> Products
);

public class GetProductsByCategoryHandler(CatalogDbContext catalogDbContext)
	: IQueryHandler<GetProductsByCategoryQuery, GetProductsByCategoryResul>
{
	public async Task<GetProductsByCategoryResul> Handle(GetProductsByCategoryQuery comamnd, CancellationToken cancellationToken)
	{
		List<Product> products = await catalogDbContext.Products
			.AsNoTracking()
			.Where(x => x.Category.Contains(comamnd.Category))
			.OrderBy(p => p.Name)
			.ToListAsync(cancellationToken);

		List<ProductDto> productsDto = products.Adapt<List<ProductDto>>();

		return new GetProductsByCategoryResul(productsDto);
	}

}
