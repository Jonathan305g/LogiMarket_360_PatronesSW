using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Orders.Memento;

// Una "fotografía" de un borrador en un instante.
public sealed class PedidoMemento
{
    // "internal": el exterior puede guardar un memento, pero NO leerlo ni modificarlo.
    internal TipoEntrega Entrega { get; }
    internal IReadOnlyList<PedidoDetalle> Detalles { get; }

    internal PedidoMemento(TipoEntrega entrega, IEnumerable<PedidoDetalle> detalles)
    {
        Entrega = entrega;

        // Copia profunda: si el pedido cambia después, la fotografía no se altera.
        Detalles = detalles.Select(d => d.Clonar()).ToList();
    }
}