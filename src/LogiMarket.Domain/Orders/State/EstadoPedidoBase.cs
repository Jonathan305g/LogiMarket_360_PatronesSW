using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Orders.State;

internal abstract class EstadoPedidoBase : IEstadoPedido
{
    public abstract EstadoPedido Estado { get; }

    // Por defecto TODA acción está prohibida. Cada estado concreto sobrescribe
    // únicamente las acciones que sí permite.
    public virtual IEstadoPedido Validar() => throw Rechazar("validar");
    public virtual IEstadoPedido Pagar() => throw Rechazar("pagar");
    public virtual IEstadoPedido Despachar() => throw Rechazar("despachar");
    public virtual IEstadoPedido Entregar() => throw Rechazar("entregar"); 
    public virtual IEstadoPedido Cancelar() => throw Rechazar("cancelar");

    private DomainException Rechazar(string accion) =>
        new($"No se puede {accion} un pedido en estado {Estado}.");
}