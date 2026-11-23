using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Repositories;

public interface IMovimientoInventarioRepository
{
    Task<MovimientoInventario?> ObtenerPorIdAsync(Guid id);
}