using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class ProductoRepositorioEf(LogiMarketDbContext db) : IProductoRepository
{
    public async Task<IReadOnlyList<Producto>> ObtenerPorSkusAsync(IEnumerable<string> skus)
    {
        ArgumentNullException.ThrowIfNull(skus);
        var lista = skus.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (lista.Length == 0)
            return Array.Empty<Producto>();

        return await db.Productos.Where(p => lista.Contains(p.Sku)).ToListAsync();
    }
}
