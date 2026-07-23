using Catalog.Data;
using MediatR;
using Catalog.Contracts.Products.Dtos;
using Catalog.Contracts.Products.Features.GetProductById;
using Catalog.Products.Models;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.GraphQL;

public sealed class CatalogQueries
{
  [UseOffsetPaging(IncludeTotalCount = true, MaxPageSize = 20)]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Product> Products([Service] CatalogDbContext catalogDbContext)
  {
    return catalogDbContext.Products
      .AsNoTracking();
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
