namespace Catalog.Products.DTOs;

public record ProductDto(
	Guid Id,
	String Name,
	List<String> Category,
	String Description,
	Decimal Price,
	String ImageFile
);


