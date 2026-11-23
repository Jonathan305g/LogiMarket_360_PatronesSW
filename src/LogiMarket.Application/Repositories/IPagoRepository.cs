using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Repositories;

public interface IPagoRepository
{
    Task<Pago?> ObtenerPorIdAsync(Guid id);
}