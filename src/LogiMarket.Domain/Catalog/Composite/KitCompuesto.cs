using LogiMarket.Domain.Catalog.Iterator;
using LogiMarket.Domain.Catalog.Visitor;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Catalog.Composite;

// COMPUESTO: contiene productos u otros kits.
public class KitCompuesto : IElementoCatalogo
{
    // Un componente del kit con su cantidad (ej.: 2 mouse dentro del kit).
    internal record ItemKit(IElementoCatalogo Elemento, int Cantidad);
    
    private readonly List<ItemKit> _items = new();

    internal IReadOnlyList<ItemKit> Items => _items;

    public string Sku { get; }
    public string Nombre { get; }

    public KitCompuesto(string sku, string nombre)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("El SKU del kit es obligatorio.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del kit es obligatorio.");

        Sku = sku.Trim();
        Nombre = nombre.Trim();
    }

    public void Agregar(IElementoCatalogo elemento, int cantidad = 1)
    {
        ArgumentNullException.ThrowIfNull(elemento);

        if (cantidad <= 0)
            throw new DomainException("La cantidad en el kit debe ser mayor que cero.");

        // Detección de ciclos: si lo que voy a agregar YA contiene a este kit
        // (o es este mismo kit), se formaría un círculo.
        // Ej.: A contiene B; si ahora B.Agregar(A) -> A contiene B contiene A.
        if (elemento.ContieneSku(Sku))
            throw new DomainException($"No se puede agregar '{elemento.Sku}' a '{Sku}': formaría una referencia circular.");

        _items.Add(new ItemKit(elemento, cantidad));
    }

    // Precio = suma de (precio de cada parte x su cantidad).
    // Cada parte pregunta su precio: si es un kit, este método se vuelve a ejecutar adentro (recursión).
    public Dinero ObtenerPrecio()
    {
        var total = Dinero.Cero;
        foreach (var item in _items)
            total += item.Elemento.ObtenerPrecio() * item.Cantidad;
        return total;
    }

    // Baja hasta los SKU finales y multiplica las cantidades por cada nivel.
    public IEnumerable<ComponenteFisico> ExpandirComponentes()
    {
        foreach (var item in _items)
            foreach (var componente in item.Elemento.ExpandirComponentes())
                yield return componente with { Cantidad = componente.Cantidad * item.Cantidad };
    }

    public bool ContieneSku(string sku) =>
        string.Equals(Sku, sku, StringComparison.OrdinalIgnoreCase)
        || _items.Any(i => i.Elemento.ContieneSku(sku));

    // Iterator: el kit fabrica su propio iterador.
    public IIteradorComponentes CrearIterador() => new IteradorComponentesKit(this);

    // Visitor: se presenta ante el visitante y luego deja que cada hijo haga lo mismo,
    // propagando la cantidad multiplicada.
    public void Aceptar(IVisitorCatalogo visitor, int cantidad)
    {
        visitor.Visitar(this, cantidad);

        foreach (var item in _items)
            item.Elemento.Aceptar(visitor, cantidad * item.Cantidad);
    }

}