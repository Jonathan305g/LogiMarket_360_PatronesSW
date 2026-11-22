using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Orders.State;

internal sealed class EstadoValidado : EstadoPedidoBase
{
    public override EstadoPedido Estado => EstadoPedido.Validado;

    public override IEstadoPedido Pagar() => new EstadoPagado();
    public override IEstadoPedido Cancelar() => new EstadoCancelado();
} 