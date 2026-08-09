using HotChocolate;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

namespace Catalog.GraphQL;

[Key("id")]
public sealed class ProductListItem
{
  public Guid Id { get; init; }

  public string Name { get; init; } = string.Empty;

  public List<string> Category { get; init; } = [];

  public string Description { get; init; } = string.Empty;

  public string ImageFile { get; init; } = string.Empty;

  public decimal Price { get; init; }

  [ReferenceResolver]
  public static async Task<ProductListItem?> ResolveReferenceAsync(
    Guid id,
    [Service] CatalogDbContext dbContext,
    CancellationToken cancellationToken)
  {
    return await dbContext.Products
      .AsNoTracking()
      .Where(p => p.Id == id)
      .Select(p => new ProductListItem
      {
        Id = p.Id,
        Name = p.Name,
        Category = p.Category,
        Description = p.Description,
        ImageFile = p.ImageFile,
        Price = p.Price
      })
      .FirstOrDefaultAsync(cancellationToken);
  }
}
