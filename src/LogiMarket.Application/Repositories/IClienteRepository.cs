using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Abstractions.Repositories;

public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(Guid id);
}