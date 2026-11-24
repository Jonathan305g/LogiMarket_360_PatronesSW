using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class PedidoRepositorioEf(LogiMarketDbContext db) : IPedidoRepository
{
    public void Agregar(Pedido pedido) => db.Pedidos.Add(pedido);

    public async Task<IReadOnlyList<ResumenPedido>> ListarResumenAsync()
    {
        var pedidos = await db.Pedidos.AsNoTracking().Include(p => p.Detalles).ToListAsync();
        var clientes = await db.Clientes.AsNoTracking().ToDictionaryAsync(c => c.Id);

        return pedidos.Select(p => new ResumenPedido(
                p.Id,
                clientes.TryGetValue(p.ClienteId, out var cliente) ? cliente.Nombre : "(desconocido)",
                p.Estado,
                p.Total.Valor))
            .OrderByDescending(p => p.Id)
            .ToArray();
    }

    public Task<Pedido?> ObtenerPorIdAsync(Guid id) => db.Pedidos
        .Include(p => p.Detalles).ThenInclude(d => d.Componentes)
        .Include(p => p.Historial)
        .FirstOrDefaultAsync(p => p.Id == id);
}
