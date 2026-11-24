using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Abstractions.Repositories;

public interface IMovimientoInventarioRepository
{
    void Agregar(MovimientoInventario movimiento);
    Task<IReadOnlyList<MovimientoInventario>> ListarAsync();
}
