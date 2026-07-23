using Catalog.Products.Models;
using HotChocolate.Types;

namespace Api.GraphQL;

public sealed class ProductType : ObjectType<Product>
{
  protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
  {
    _ = descriptor.BindFieldsExplicitly();
    _ = descriptor.Field(product => product.Id);
    _ = descriptor.Field(product => product.Name);
    _ = descriptor.Field(product => product.Category);
    _ = descriptor.Field(product => product.Description);
    _ = descriptor.Field(product => product.ImageFile);
    _ = descriptor.Field(product => product.Price);
  }
}