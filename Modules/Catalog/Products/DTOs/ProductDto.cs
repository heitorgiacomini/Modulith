namespace Catalog.Products.Dtos;

public record ProductDto(
	Guid Id,
	String Name,
	List<String> Category,
	String Description,
	Decimal Price,
	String ImageFile
);


