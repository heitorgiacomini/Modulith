namespace Catalog.Products.Features.GetProductById;

//public record GetProductByIdRequest(Guid Id);
public record GetProductByIdResponse(
	ProductDto Product);

public class GetProductByIdEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		_ = app.MapGet("/products/{id}", async (Guid id, ISender sender) =>
		{
			GetProductByIdResult result = await sender.Send(new GetProductByIdQuery(id));

			GetProductByIdResponse response = result.Adapt<GetProductByIdResponse>();

			return Results.Ok(response);


		})
		.WithName("GetProductById")
		.Produces<GetProductByIdResponse>(StatusCodes.Status200OK)
		.ProducesProblem(StatusCodes.Status400BadRequest)
		.WithSummary("Get Product By Id")
		.WithDescription("Get Product By Id");
	}
}
