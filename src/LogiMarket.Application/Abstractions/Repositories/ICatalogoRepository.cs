using LogiMarket.Domain.Catalog.Composite;

namespace LogiMarket.Application.Abstractions.Repositories;

// Resuelve productos o kits persistidos como elementos del Composite de catálogo.
public interface ICatalogoRepository
{
    Task<IElementoCatalogo?> ObtenerPorSkuAsync(string sku);
}
