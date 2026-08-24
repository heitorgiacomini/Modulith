using Catalog.Contracts.Products.Features.GetProductById;
using Catalog.Products.Features.CreateProduct;
using Catalog.Products.Features.DeleteProduct;
using Catalog.Products.Features.GetProductByCategory;
using Catalog.Products.Features.GetProductById;
using Catalog.Products.Features.GetProducts;
using Catalog.Products.Features.UpdateProduct;
using Riok.Mapperly.Abstractions;

namespace Catalog.Products.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class CatalogMapper
{
    public static partial ProductDto ToDto(Product product);

    public static partial List<ProductDto> ToDtos(List<Product> products);

    public static partial CreateProductCommand ToCommand(CreateProductRequest request);

    public static partial UpdateProductCommand ToCommand(UpdateProductRequest request);

    public static partial CreateProductResponse ToResponse(CreateProductResult result);

    public static partial UpdateProductResponse ToResponse(UpdateProductResult result);

    public static partial DeleteProductResponse ToResponse(DeleteProductResult result);

    public static partial GetProductByIdResponse ToResponse(GetProductByIdResult result);

    public static partial GetProductsResponse ToResponse(GetProductsResult result);

    public static partial GetProductByCategoryResponse ToResponse(GetProductsByCategoryResult result);
}
