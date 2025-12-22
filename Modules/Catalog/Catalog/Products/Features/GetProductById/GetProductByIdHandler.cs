namespace Catalog.Products.Features.GetProductById;

//public record GetProductByIdQuery(Guid Id)
//	: IQuery<GetProductByIdResult>;

//public record GetProductByIdResult(
//	ProductDto Product
//);

public class GetProductsByIdHandler(CatalogDbContext catalogDbContext)
    : IQueryHandler<GetProductByIdQuery, GetProductByIdResult>
{
  public async Task<GetProductByIdResult> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
  {
    Product? product = await catalogDbContext.Products
        .AsNoTracking()
        .SingleOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

    if (product == null)
    {
      throw new ProductNotFoundException(query.Id);
    }
    ProductDto productsDto = product.Adapt<ProductDto>();

    return new GetProductByIdResult(productsDto);
  }

}
