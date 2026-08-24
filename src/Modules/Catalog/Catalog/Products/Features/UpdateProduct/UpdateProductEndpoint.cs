namespace Catalog.Products.Features.UpdateProduct;

public record UpdateProductRequest(ProductDto Product);
public record UpdateProductResponse(bool IsSuccess);

public class UpdateProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/products", async (UpdateProductRequest request, ISender sender) =>
        {
            var command = CatalogMapper.ToCommand(request);

            var result = await sender.Send(command);

            var response = CatalogMapper.ToResponse(result);

            return Results.Ok(response);
        })
        .WithName("UpdateProduct")
        .RequireAuthorization(CatalogAuthorization.AdminPolicy)
        .Produces<UpdateProductResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update Product")
        .WithDescription("Update Product");
    }
}
