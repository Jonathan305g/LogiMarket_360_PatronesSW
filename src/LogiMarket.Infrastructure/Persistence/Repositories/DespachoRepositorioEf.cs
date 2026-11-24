using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class DespachoRepositorioEf(LogiMarketDbContext db) : IDespachoRepository
{
    public void Agregar(Despacho despacho) => db.Despachos.Add(despacho);
}
