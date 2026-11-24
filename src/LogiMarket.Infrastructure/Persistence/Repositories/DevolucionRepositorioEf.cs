using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class DevolucionRepositorioEf(LogiMarketDbContext db) : IDevolucionRepository
{
    public void Agregar(Devolucion devolucion) => db.Devoluciones.Add(devolucion);
}
