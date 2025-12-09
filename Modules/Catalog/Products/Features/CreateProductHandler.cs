using MediatR;

namespace Catalog.Products.Features;

public record CreateProductCommand
	(string name, List<string> category, string description, string imageFile, decimal price)
	: IRequest<CreateProductResult>;

public record CreateProductResult(Guid id);
public class CreateProductHandler : IRequestHandler<CreateProductCommand, CreateProductResult>
{
	public Task<CreateProductResult> Handle(CreateProductCommand command, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}
