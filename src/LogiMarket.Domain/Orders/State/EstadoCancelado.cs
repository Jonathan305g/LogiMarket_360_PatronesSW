using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Orders.State;

// Estado final: un pedido cancelado no se puede reactivar.
internal sealed class EstadoCancelado : EstadoPedidoBase
{
    public override EstadoPedido Estado => EstadoPedido.Cancelado; 
}