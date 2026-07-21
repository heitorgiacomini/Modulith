using MediatR;
using Catalog.Contracts.Products.Dtos;
using Catalog.Contracts.Products.Features.GetProductById;
using Catalog.Products.Features.GetProducts;
using Shared.Pagination;

namespace Api.GraphQL;

public sealed class CatalogQueries
{
  public async Task<PaginatedResult<ProductDto>> Products(
    [Service] ISender sender,
    CancellationToken cancellationToken,
    int pageIndex = 0,
    int pageSize = 10)
  {
    GetProductsResult result = await sender.Send(
      new GetProductsQuery(new PaginationRequest(pageIndex, pageSize)),
      cancellationToken);

    return result.Products;
  }

  public async Task<ProductDto?> Product(
    Guid id,
    [Service] ISender sender,
    CancellationToken cancellationToken)
  {
    GetProductByIdResult result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
    return result.Product;
  }
}
