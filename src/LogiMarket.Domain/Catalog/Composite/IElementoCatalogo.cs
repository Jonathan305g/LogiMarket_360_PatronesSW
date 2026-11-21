using LogiMarket.Domain.Catalog.Visitor;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Catalog.Composite;

// Es el contrato COMÚN para un producto suelto y para un kit.
// Quien lo use no necesita saber cuál de los dos tiene delante.
public interface IElementoCatalogo
{
    string Sku { get; }
    string Nombre { get; }

    // Precio de una unidad (en un kit: suma recursiva de sus partes).
    Dinero ObtenerPrecio();

    // Lista de productos físicos finales con su cantidad.
    IEnumerable<ComponenteFisico> ExpandirComponentes();
    bool ContieneSku(string sku);

    // Punto de entrada del Visitor.
    void Aceptar(IVisitorCatalogo visitor, int cantidad);
}