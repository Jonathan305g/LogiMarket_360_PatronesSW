using LogiMarket.Domain.Catalog.Composite;

namespace LogiMarket.Application.Abstractions.Repositories;

public sealed record ResumenElementoCatalogo(string Sku, string Nombre, bool EsKit, int? Stock);

// Resuelve productos o kits persistidos como elementos del Composite de catálogo.
public interface ICatalogoRepository
{
    Task<IElementoCatalogo?> ObtenerPorSkuAsync(string sku);
    Task<IReadOnlyList<ResumenElementoCatalogo>> ListarResumenAsync(CancellationToken ct = default);
}
