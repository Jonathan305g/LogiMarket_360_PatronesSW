using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Orders.State;

// Reconstruye el objeto de ESTADO correcto a partir del enum guardado en la base.
// Lo usa SOLO el puente de persistencia de Pedido (ver más abajo), nunca el negocio.
internal static class FabricaEstados
{
    public static IEstadoPedido Reconstruir(EstadoPedido estado) => estado switch
    {
        EstadoPedido.Borrador => new EstadoBorrador(),
        EstadoPedido.Validado => new EstadoValidado(),
        EstadoPedido.Pagado => new EstadoPagado(),
        EstadoPedido.Despachado => new EstadoDespachado(),
        EstadoPedido.Entregado => new EstadoEntregado(),
        EstadoPedido.Cancelado => new EstadoCancelado(),
        _ => throw new ArgumentOutOfRangeException(nameof(estado), $"Estado no reconocido: {estado}.")
    };
}