namespace Catalog.Products.Features.GetProductByCategory;

//public record GetProductByCategoryRequest(string Category);
public record GetProductByCategoryResponse(IEnumerable<ProductDto> Products);

public class GetProductByCategoryEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		_ = app.MapGet("/products/category/{category}", async (String category, ISender sender) =>
		{
			var result = await sender.Send(new GetProductByCategoryQuery(category));

			GetProductByCategoryResponse response = result.Adapt<GetProductByCategoryResponse>();

			return Results.Ok(response);
		})
		.WithName("GetProductByCategory")
		.Produces<GetProductByCategoryResponse>(StatusCodes.Status200OK)
		.ProducesProblem(StatusCodes.Status400BadRequest)
		.WithSummary("Get Product By Category")
		.WithDescription("Get Product By Category");
	}
}
