using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Orders.Builder;

public class PedidoBuilder
{
    private Guid _clienteId = Guid.Empty;
    private TipoEntrega _entrega = TipoEntrega.Estandar;
    private readonly List<(IElementoCatalogo Elemento, int Cantidad)> _lineas = new();

    // Cada paso devuelve "this" para poder encadenar las llamadas.
    public PedidoBuilder ConCliente(Cliente cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        _clienteId = cliente.Id;
        return this;
    }

    public PedidoBuilder ConEntrega(TipoEntrega entrega)
    {
        _entrega = entrega;
        return this;
    }

    // Acepta un producto simple o un kit (ambos son IElementoCatalogo).
    public PedidoBuilder AgregarLinea(IElementoCatalogo elemento, int cantidad)
    {
        ArgumentNullException.ThrowIfNull(elemento);
        _lineas.Add((elemento, cantidad));
        return this;
    }

    // El único punto donde nace el pedido: o sale completo y válido, o lanza error.
    public Pedido Construir()
    {
        if (_clienteId == Guid.Empty)
            throw new DomainException("El pedido requiere un cliente.");

        if (_lineas.Count == 0)
            throw new DomainException("El pedido requiere al menos una línea.");

        var pedido = new Pedido(_clienteId, _entrega);
        foreach (var (elemento, cantidad) in _lineas)
            pedido.AgregarLinea(elemento, cantidad); // valida cantidades y toma las instantáneas

        return pedido;
    }
}