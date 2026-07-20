using Catalog.Data;
using Catalog.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.GraphQL;

public sealed class CatalogQueries
{
    public async Task<List<Product>> GetProducts(
        [Service] CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetProduct(
        Guid id,
        [Service] CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }
}
