using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class DevolucionRepositorioEf(LogiMarketDbContext db) : IDevolucionRepository
{
    public void Agregar(Devolucion devolucion) => db.Devoluciones.Add(devolucion);

    public async Task<IReadOnlyList<Devolucion>> ListarAsync(CancellationToken ct = default) =>
        await db.Devoluciones.AsNoTracking().ToListAsync(ct);
}
