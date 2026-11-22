using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Orders.State;

internal sealed class EstadoPagado : EstadoPedidoBase
{
    public override EstadoPedido Estado => EstadoPedido.Pagado;

    // Solo se puede despachar. Cancelar queda prohibido (lo hereda de la base).
    public override IEstadoPedido Despachar() => new EstadoDespachado();
}