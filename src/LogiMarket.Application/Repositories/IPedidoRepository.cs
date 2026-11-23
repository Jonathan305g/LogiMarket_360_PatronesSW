using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Abstractions.Repositories;

public interface IPedidoRepository
{
    Task<Pedido?> ObtenerPorIdAsync(Guid id);
}