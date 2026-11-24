using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Entities;

// Una línea del pedido. Guarda una INSTANTÁNEA: el precio y la composición del
// producto o kit tal como estaban al agregarlo.|
public class PedidoDetalle
{
    private readonly List<ComponenteFisico> _componentes;

    public Guid Id { get; private set; }
    public string Sku { get; private set; }
    public string Nombre { get; private set; }

    // Precio de UNA unidad, congelado al agregar la línea.
    public Dinero PrecioUnitario { get; private set; }
    public int Cantidad { get; private set; }

    // SKU físicos de UNA unidad de la línea (para un producto simple: él mismo x1).
    // Con esto se descontará el stock sin volver a consultar el catálogo.
    public IReadOnlyList<ComponenteFisico> Componentes => _componentes;

    public Dinero Subtotal => PrecioUnitario * Cantidad;

    private PedidoDetalle() { Sku = string.Empty; Nombre = string.Empty; _componentes = new(); } // EF Core

    // "internal": solo Pedido (mismo proyecto) puede crear y modificar líneas.
    internal PedidoDetalle(string sku, string nombre, Dinero precioUnitario, int cantidad,
        IEnumerable<ComponenteFisico> componentes)
    {
        if (cantidad <= 0)
            throw new DomainException("La cantidad debe ser mayor que cero.");

        Id = Guid.NewGuid();
        Sku = sku;
        Nombre = nombre;
        PrecioUnitario = precioUnitario;
        Cantidad = cantidad;
        _componentes = componentes.ToList(); // copia propia de la lista
    }

    internal void CambiarCantidad(int cantidad)
    {
        if (cantidad <= 0)
            throw new DomainException("La cantidad debe ser mayor que cero.");

        Cantidad = cantidad;
    }

    // Copia profunda: Id nuevo y lista de componentes nueva (el constructor hace ToList).
    internal PedidoDetalle Clonar() => new(Sku, Nombre, PrecioUnitario, Cantidad, _componentes);
}
