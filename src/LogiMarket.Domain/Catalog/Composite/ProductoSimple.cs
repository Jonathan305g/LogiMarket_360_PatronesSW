using LogiMarket.Domain.Catalog.Visitor;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Catalog.Composite;

public class ProductoSimple : IElementoCatalogo
{
    private readonly Producto _producto;

    public ProductoSimple(Producto producto)
    {
        _producto = producto ?? throw new ArgumentNullException(nameof(producto));
    }

    public string Sku => _producto.Sku;
    public string Nombre => _producto.Nombre;

    // Visitor necesita leer el peso del producto
    public decimal PesoKg => _producto.PesoKg;

    public Dinero ObtenerPrecio() => _producto.Precio;

    // Un producto simple se expande a sí mismo: 1 unidad de su SKU.
    public IEnumerable<ComponenteFisico> ExpandirComponentes()
    {
        yield return new ComponenteFisico(Sku, 1);
    }

    public bool ContieneSku(string sku) =>
        string.Equals(Sku, sku, StringComparison.OrdinalIgnoreCase);

    // Punto de entrada del Visitor: se le entrega a él mismo y la cantidad que se necesita.
    public void Aceptar(IVisitorCatalogo visitor, int cantidad) =>
       visitor.Visitar(this, cantidad);
}