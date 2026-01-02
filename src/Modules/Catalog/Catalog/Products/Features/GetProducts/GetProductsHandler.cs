using Shared.Pagination;

namespace Catalog.Products.Features.GetProducts;


public record GetProductsQuery(PaginationRequest PaginationRequest)
		: IQuery<GetProductsResult>;
public record GetProductsResult(
	PaginatedResult<ProductDto> Products
);

public class GetProductsHandler(CatalogDbContext catalogDbContext)
	: IQueryHandler<GetProductsQuery, GetProductsResult>
{
	public async Task<GetProductsResult> Handle(GetProductsQuery query, CancellationToken cancellationToken)
	{
		Int32 pageIndex = query.PaginationRequest.PageIndex;
		Int32 pageSize = query.PaginationRequest.PageSize;

		Int64 totalCount = await catalogDbContext.Products.LongCountAsync(cancellationToken);

		var products = await catalogDbContext.Products
										.AsNoTracking()
										.OrderBy(p => p.Name)
										.Skip(pageSize * pageIndex)
										.Take(pageSize)
										.ToListAsync(cancellationToken);

		//mapping product entity to ProductDto using Mapster
		var productDtos = products.Adapt<List<ProductDto>>();

		return new GetProductsResult(
				new PaginatedResult<ProductDto>(
						pageIndex,
						pageSize,
						totalCount,
						productDtos)
				);
	}

}
