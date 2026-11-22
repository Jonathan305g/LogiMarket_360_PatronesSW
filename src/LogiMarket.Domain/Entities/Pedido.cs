using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Orders.Prototype;
using LogiMarket.Domain.Orders.State;
using LogiMarket.Domain.ValueObjects;
using LogiMarket.Domain.Orders.Memento;

namespace LogiMarket.Domain.Entities;

public class Pedido : IPrototipo<Pedido>
{
    private IEstadoPedido _estado = new EstadoBorrador();
    private readonly List<CambioEstado> _historial = new();
    private readonly List<PedidoDetalle> _detalles = new();

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public TipoEntrega Entrega { get; private set; }
    public Dinero Descuento { get; private set; } = Dinero.Cero;
    public Dinero Tarifa { get; private set; } = Dinero.Cero;
    public DateTime? FechaEntregaUtc { get; private set; }
    public bool Devuelto { get; private set; }

    public EstadoPedido Estado => _estado.Estado;
    public IReadOnlyList<CambioEstado> Historial => _historial;
    public IReadOnlyList<PedidoDetalle> Detalles => _detalles;

    public Dinero Subtotal
    {
        get
        {
            var total = Dinero.Cero;
            foreach (var detalle in _detalles)
                total += detalle.Subtotal;
            return total;
        }
    }

    // productos con descuento (base del reembolso, sin tarifa).
    public Dinero SubtotalConDescuento => Subtotal - Descuento;

    // total = subtotal - descuento + tarifa.
    public Dinero Total => SubtotalConDescuento + Tarifa;

    // Unidades físicas finales (un kit cuenta por sus SKU): lo usará la política MAYORISTA.
    public int UnidadesFisicas =>
        _detalles.Sum(d => d.Cantidad * d.Componentes.Sum(c => c.Cantidad));

    // "internal": desde fuera de Domain solo se crea con PedidoBuilder (o clonando).
    internal Pedido(Guid clienteId, TipoEntrega entrega)
    {
        if (clienteId == Guid.Empty)
            throw new DomainException("El pedido requiere un cliente.");

        Id = Guid.NewGuid();
        ClienteId = clienteId;
        Entrega = entrega;
    }

    // ---------- Edición del borrador ----------

    public void AgregarLinea(IElementoCatalogo elemento, int cantidad)
    {
        ArgumentNullException.ThrowIfNull(elemento);
        AsegurarBorrador();

        if (cantidad <= 0)
            throw new DomainException("La cantidad debe ser mayor que cero.");

        // Si el SKU ya está en el pedido, se suma a la línea existente.
        var existente = BuscarLinea(elemento.Sku);
        if (existente is not null)
        {
            existente.CambiarCantidad(existente.Cantidad + cantidad);
            return;
        }

        // Instantánea: se expande el kit y se consolidan los SKU repetidos.
        var componentes = elemento.ExpandirComponentes()
            .GroupBy(c => c.Sku)
            .Select(g => new ComponenteFisico(g.Key, g.Sum(c => c.Cantidad)))
            .ToList();

        _detalles.Add(new PedidoDetalle(
            elemento.Sku, elemento.Nombre, elemento.ObtenerPrecio(), cantidad, componentes));
    }

    public void CambiarCantidad(string sku, int cantidad)
    {
        AsegurarBorrador();

        var linea = BuscarLinea(sku)
            ?? throw new DomainException($"El pedido no tiene una línea con SKU '{sku}'.");

        linea.CambiarCantidad(cantidad);
    }

    public void CambiarEntrega(TipoEntrega entrega)
    {
        AsegurarBorrador();
        Entrega = entrega;
    }

    // ---------- Componentes físicos ----------

    // SKU finales de TODO el pedido, con cantidades ya multiplicadas y consolidadas.
    // Sirve para descontar el stock al cobrar y para reintegrarlo al devolver.
    public IReadOnlyList<ComponenteFisico> ComponentesFisicosTotales() =>
        _detalles
            .SelectMany(d => d.Componentes.Select(c => new ComponenteFisico(c.Sku, c.Cantidad * d.Cantidad)))
            .GroupBy(c => c.Sku)
            .Select(g => new ComponenteFisico(g.Key, g.Sum(c => c.Cantidad)))
            .ToList();

    // ---------- Prototype ----------

    // Copia de plantilla: solo desde BORRADOR. Nace con Id nuevo, estado BORRADOR,
    // sin historial y con líneas propias (copia profunda).
    public Pedido Clonar()
    {
        AsegurarBorrador();

        var copia = new Pedido(ClienteId, Entrega);
        foreach (var detalle in _detalles)
            copia._detalles.Add(detalle.Clonar());
        return copia;
    }

    // ---------- Memento ----------

    // "internal": solo HistorialEdiciones (dentro de Domain) puede sacar y devolver fotografías.
    internal PedidoMemento CrearMemento()
    {
        AsegurarBorrador();
        return new PedidoMemento(Entrega, _detalles);
    }

    internal void Restaurar(PedidoMemento memento)
    {
        AsegurarBorrador();

        Entrega = memento.Entrega;
        _detalles.Clear();
        foreach (var detalle in memento.Detalles)
            _detalles.Add(detalle.Clonar()); // se vuelve a clonar para no compartir objetos con el memento
    }

    // ---------- State ----------

    public void Validar() => Transicionar(_estado.Validar());
    public void Pagar() => Transicionar(_estado.Pagar());
    public void Despachar() => Transicionar(_estado.Despachar());
    public void Entregar()
    {
        Transicionar(_estado.Entregar());
        FechaEntregaUtc = DateTime.UtcNow;
    }

    public void Cancelar() => Transicionar(_estado.Cancelar());

    private void Transicionar(IEstadoPedido nuevo)
    {
        var anterior = _estado.Estado;
        _estado = nuevo;
        _historial.Add(new CambioEstado(anterior, nuevo.Estado, DateTime.UtcNow));
    }

    // ---------- Devolución ----------

    // "internal": solo el proceso de devolución puede marcar un pedido como devuelto.
    internal void MarcarDevuelto()
    {
        if (Estado != EstadoPedido.Entregado)
            throw new DomainException("Solo se puede devolver un pedido ENTREGADO.");

        if (Devuelto)
            throw new DomainException("El pedido ya fue devuelto.");

        Devuelto = true;
    }

    // ---------- Auxiliares ----------

    private PedidoDetalle? BuscarLinea(string sku) =>
        _detalles.FirstOrDefault(d => string.Equals(d.Sku, sku, StringComparison.OrdinalIgnoreCase));

    // Solo un BORRADOR se puede modificar: un pedido validado o pagado queda cerrado.
    private void AsegurarBorrador()
    {
        if (Estado != EstadoPedido.Borrador)
            throw new DomainException($"El pedido está {Estado}: solo se puede modificar en BORRADOR.");
    }
}