using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Orders.State;

namespace LogiMarket.Domain.Entities;

public class Pedido
{
    private IEstadoPedido _estado = new EstadoBorrador();
    private readonly List<CambioEstado> _historial = new();

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }

    // El mundo exterior ve el enum; el comportamiento vive en el objeto de estado.
    public EstadoPedido Estado => _estado.Estado;

    // Solo lectura: nadie puede agregar ni borrar cambios a mano.
    public IReadOnlyList<CambioEstado> Historial => _historial;

    public Pedido(Guid clienteId)
    {
        if (clienteId == Guid.Empty)
            throw new DomainException("El pedido requiere un cliente.");

        Id = Guid.NewGuid();
        ClienteId = clienteId;
    }

    public void Validar() => Transicionar(_estado.Validar());
    public void Pagar() => Transicionar(_estado.Pagar());
    public void Despachar() => Transicionar(_estado.Despachar());
    public void Entregar() => Transicionar(_estado.Entregar());
    public void Cancelar() => Transicionar(_estado.Cancelar());

    // Solo se llega aquí si el estado actual ACEPTÓ la acción; si la rechazó,
    // lanzó la excepción antes y no se cambió nada ni se registró nada.
    private void Transicionar(IEstadoPedido nuevo)
    {
        var anterior = _estado.Estado;
        _estado = nuevo;
        _historial.Add(new CambioEstado(anterior, nuevo.Estado, DateTime.UtcNow));
    }
}