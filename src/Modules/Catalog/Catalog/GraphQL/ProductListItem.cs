using HotChocolate.Types;
using HotChocolate.Types.Composite;

namespace Catalog.GraphQL;

public sealed class ProductListItem
{
  public Guid Id { get; init; }

  public string Name { get; init; } = string.Empty;

  public List<string> Category { get; init; } = [];

  public string Description { get; init; } = string.Empty;

  public string ImageFile { get; init; } = string.Empty;

  public decimal Price { get; init; }
}

public sealed class ProductListItemType : ObjectType<ProductListItem>
{
  protected override void Configure(IObjectTypeDescriptor<ProductListItem> descriptor)
  {
    ((IObjectTypeDescriptor)descriptor).EntityKey("id");
  }
}
