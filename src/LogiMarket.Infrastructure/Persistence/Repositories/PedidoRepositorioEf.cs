using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class PedidoRepositorioEf(LogiMarketDbContext db) : IPedidoRepository
{
    public void Agregar(Pedido pedido) => db.Pedidos.Add(pedido);

    public Task<Pedido?> ObtenerPorIdAsync(Guid id) => db.Pedidos
        .Include(p => p.Detalles).ThenInclude(d => d.Componentes)
        .Include(p => p.Historial)
        .FirstOrDefaultAsync(p => p.Id == id);
}
