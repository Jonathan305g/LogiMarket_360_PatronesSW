using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Abstractions.Repositories;

public sealed record ResumenPedido(Guid Id, string Cliente, EstadoPedido Estado, decimal Total)
{
    public override string ToString() => $"{Cliente} | {Estado} | {Total:F2} | {Id.ToString()[..8]}";
}

public interface IPedidoRepository
{
    Task<Pedido?> ObtenerPorIdAsync(Guid id);
    void Agregar(Pedido pedido);
    Task<IReadOnlyList<ResumenPedido>> ListarResumenAsync();
}
