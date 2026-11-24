using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class CatalogoRepositorioEf(LogiMarketDbContext db) : ICatalogoRepository
{
    public Task<IElementoCatalogo?> ObtenerPorSkuAsync(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        return ResolverAsync(sku.Trim(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private async Task<IElementoCatalogo?> ResolverAsync(string sku, HashSet<string> rutaActual)
    {
        if (!rutaActual.Add(sku))
            throw new DomainException($"Ciclo detectado en la definición de kits al resolver '{sku}'.");

        try
        {
            var kit = await db.Kits.AsNoTracking()
                .Include(k => k.Componentes)
                .FirstOrDefaultAsync(k => k.Sku == sku);

            if (kit is null)
                return await ResolverProductoAsync(sku);

            var compuesto = new KitCompuesto(kit.Sku, kit.Nombre);
            foreach (var componente in kit.Componentes)
            {
                var hijo = componente.EsKit
                    ? await ResolverAsync(componente.ComponenteSku, rutaActual)
                    : await ResolverProductoAsync(componente.ComponenteSku);

                if (hijo is null)
                    throw new DomainException($"El kit '{sku}' referencia un componente inexistente: '{componente.ComponenteSku}'.");

                compuesto.Agregar(hijo, componente.Cantidad);
            }

            return compuesto;
        }
        finally
        {
            rutaActual.Remove(sku);
        }
    }

    private async Task<IElementoCatalogo?> ResolverProductoAsync(string sku)
    {
        var producto = await db.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Sku == sku);
        return producto is null ? null : new ProductoSimple(producto);
    }
}
