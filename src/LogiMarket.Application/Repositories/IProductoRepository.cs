using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Abstractions.Repositories;

public interface IProductoRepository
{
    // Devuelve los productos reales (rastreados) para poder descontarles stock.
    Task<IReadOnlyList<Producto>> ObtenerPorSkusAsync(IEnumerable<string> skus);
}