using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class MovimientoInventarioRepositorioEf(LogiMarketDbContext db) : IMovimientoInventarioRepository
{
    public void Agregar(MovimientoInventario movimiento) => db.Movimientos.Add(movimiento);
}
