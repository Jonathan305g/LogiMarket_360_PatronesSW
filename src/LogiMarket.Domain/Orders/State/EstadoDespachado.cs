using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Orders.State;

internal sealed class EstadoDespachado : EstadoPedidoBase
{
    public override EstadoPedido Estado => EstadoPedido.Despachado;

    public override IEstadoPedido Entregar() => new EstadoEntregado();
} 