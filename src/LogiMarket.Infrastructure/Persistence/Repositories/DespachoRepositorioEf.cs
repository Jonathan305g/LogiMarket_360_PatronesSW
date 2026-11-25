using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class DespachoRepositorioEf(LogiMarketDbContext db) : IDespachoRepository
{
    public void Agregar(Despacho despacho) => db.Despachos.Add(despacho);

    public async Task<IReadOnlyList<Despacho>> ListarAsync(CancellationToken ct = default) =>
        await db.Despachos.AsNoTracking().ToListAsync(ct);
}
