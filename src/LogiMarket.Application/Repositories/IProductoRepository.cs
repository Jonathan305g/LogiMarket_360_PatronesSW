using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Repositories;

public interface IProductoRepository
{
    Task<Producto?> ObtenerPorIdAsync(Guid id);
}