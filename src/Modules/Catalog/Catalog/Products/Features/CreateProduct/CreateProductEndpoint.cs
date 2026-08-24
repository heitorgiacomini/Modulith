namespace Catalog.Products.Features.CreateProduct;

public record CreateProductRequest(ProductDto Product);
public record CreateProductResponse(Guid Id);

public class CreateProductEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		_ = app.MapPost("/products", async (CreateProductRequest request, ISender sender) =>
		{
			CreateProductCommand command = CatalogMapper.ToCommand(request);

			CreateProductResult result = await sender.Send(command);

			CreateProductResponse response = CatalogMapper.ToResponse(result);

			return Results.Created($"/products/{response.Id}", response);

		})
		.WithName("CreateProduct")
		.RequireAuthorization(CatalogAuthorization.AdminPolicy)
		.Produces<CreateProductResponse>(StatusCodes.Status201Created)
		.ProducesProblem(StatusCodes.Status400BadRequest)
		.WithSummary("Create Product")
		.WithDescription("Create Product");

	}
}
