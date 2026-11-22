using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Orders.State;

// Estado final del flujo normal: no permite ninguna acción.
// La devolución NO es una transición de estado; se trata aparte (Template Method).
internal sealed class EstadoEntregado : EstadoPedidoBase
{
    public override EstadoPedido Estado => EstadoPedido.Entregado; 
}