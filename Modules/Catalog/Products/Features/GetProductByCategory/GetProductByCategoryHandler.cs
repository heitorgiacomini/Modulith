namespace Catalog.Products.Features.GetProductByCategory;

public record GetProductByCategoryQuery(String Category)
	: IQuery<GetProductsByCategoryResult>;

public record GetProductsByCategoryResult(
	IEnumerable<ProductDto> Products
);

public class GetProductsByCategoryHandler(CatalogDbContext catalogDbContext)
	: IQueryHandler<GetProductByCategoryQuery, GetProductsByCategoryResult>
{
	public async Task<GetProductsByCategoryResult> Handle(GetProductByCategoryQuery comamnd, CancellationToken cancellationToken)
	{
		List<Product> products = await catalogDbContext.Products
			.AsNoTracking()
			.Where(x => x.Category.Contains(comamnd.Category))
			.OrderBy(p => p.Name)
			.ToListAsync(cancellationToken);

		List<ProductDto> productsDto = products.Adapt<List<ProductDto>>();

		return new GetProductsByCategoryResult(productsDto);
	}

}
