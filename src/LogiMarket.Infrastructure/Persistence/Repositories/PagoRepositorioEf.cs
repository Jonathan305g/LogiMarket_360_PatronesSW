using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class PagoRepositorioEf(LogiMarketDbContext db) : IPagoRepository
{
    public Task<Pago?> ObtenerPorClaveAsync(string claveIdempotencia) =>
        db.Pagos.FirstOrDefaultAsync(p => p.ClaveIdempotencia == claveIdempotencia);

    public void Agregar(Pago pago) => db.Pagos.Add(pago);
}
