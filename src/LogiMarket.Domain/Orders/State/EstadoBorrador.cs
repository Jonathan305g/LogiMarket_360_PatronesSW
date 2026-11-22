using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Orders.State;

internal sealed class EstadoBorrador : EstadoPedidoBase
{
    public override EstadoPedido Estado => EstadoPedido.Borrador;

    public override IEstadoPedido Validar() => new EstadoValidado();
    public override IEstadoPedido Cancelar() => new EstadoCancelado();
}