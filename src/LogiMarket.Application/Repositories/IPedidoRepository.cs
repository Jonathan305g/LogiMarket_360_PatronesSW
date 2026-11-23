using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Repositories;

public interface IPedidoRepository
{
    Task<Pedido?> ObtenerPorIdAsync(Guid id);
}