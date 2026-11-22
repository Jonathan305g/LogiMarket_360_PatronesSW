using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Orders.State;

public interface IEstadoPedido
{
    EstadoPedido Estado { get; }

    // Cada acción devuelve EL ESTADO SIGUIENTE, o lanza DomainException si no es válida.
    IEstadoPedido Validar();
    IEstadoPedido Pagar();
    IEstadoPedido Despachar();
    IEstadoPedido Entregar();
    IEstadoPedido Cancelar();
}