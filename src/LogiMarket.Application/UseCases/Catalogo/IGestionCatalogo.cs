using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Validation;

namespace LogiMarket.Application.UseCases.Catalogo;

public interface IGestionCatalogo
{
    Task<ResultadoOperacion> GuardarProductoAsync(string sku, string nombre, decimal precio, decimal pesoKg, int stockInicial, CancellationToken ct = default);
    Task<ResultadoOperacion> GuardarKitAsync(string sku, string nombre, IReadOnlyList<ComponenteKitSolicitud> componentes, CancellationToken ct = default);
    Task<ResultadoOperacion> EliminarAsync(string sku, CancellationToken ct = default);
}
