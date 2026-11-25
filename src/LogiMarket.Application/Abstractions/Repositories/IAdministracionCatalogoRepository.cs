using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Abstractions.Repositories;

public sealed record ComponenteKitSolicitud(string Sku, int Cantidad, bool EsKit);

public interface IAdministracionCatalogoRepository
{
    Task<Producto?> ObtenerProductoParaEdicionAsync(string sku, CancellationToken ct = default);
    Task<bool> SkuExisteAsync(string sku, CancellationToken ct = default);
    void AgregarProducto(Producto producto);
    Task<bool> EliminarProductoAsync(string sku, CancellationToken ct = default);
    Task GuardarKitAsync(string sku, string nombre, IReadOnlyList<ComponenteKitSolicitud> componentes, CancellationToken ct = default);
    Task<bool> EliminarKitAsync(string sku, CancellationToken ct = default);
}
