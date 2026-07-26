namespace Catalog.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class CatalogQueries
{
  [UseOffsetPaging(IncludeTotalCount = true, DefaultPageSize = 20, MaxPageSize = 20)]
  [UseFiltering]
  [UseSorting]
  public IQueryable<ProductListItem> Products([Service] CatalogDbContext catalogDbContext)
  {
    return catalogDbContext.Products
      .AsNoTracking()
      .Select(product => new ProductListItem
      {
        Id = product.Id,
        Name = product.Name,
        Category = product.Category,
        Description = product.Description,
        ImageFile = product.ImageFile,
        Price = product.Price
      });
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
