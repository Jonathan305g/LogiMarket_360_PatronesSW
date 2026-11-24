using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class MovimientoInventarioRepositorioEf(LogiMarketDbContext db) : IMovimientoInventarioRepository
{
    public void Agregar(MovimientoInventario movimiento) => db.Movimientos.Add(movimiento);

    public async Task<IReadOnlyList<MovimientoInventario>> ListarAsync() =>
        await db.Movimientos.AsNoTracking().OrderByDescending(m => m.FechaUtc).ToListAsync();
}
