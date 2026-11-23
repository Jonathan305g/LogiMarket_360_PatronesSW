using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Repositories;

public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(Guid id);
}