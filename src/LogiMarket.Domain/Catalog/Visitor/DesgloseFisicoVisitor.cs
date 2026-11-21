using LogiMarket.Domain.Catalog.Composite;

namespace LogiMarket.Domain.Catalog.Visitor;

// Reporte 1: cuántas unidades físicas de cada SKU hay en un kit.
public class DesgloseFisicoVisitor : IVisitorCatalogo
{
    private readonly Dictionary<string, int> _cantidades = new();

    // Se entrega como solo lectura para que nadie la modifique desde fuera.
    public IReadOnlyDictionary<string, int> Desglose => _cantidades;

    public void Visitar(ProductoSimple producto, int cantidad)
    {
        // Si el SKU ya estaba, se suma; si no, parte de 0.
        _cantidades[producto.Sku] = _cantidades.GetValueOrDefault(producto.Sku) + cantidad;
    }

    // Un kit no es un producto físico: no aporta nada al desglose.
    public void Visitar(KitCompuesto kit, int cantidad)
    {
    }
}