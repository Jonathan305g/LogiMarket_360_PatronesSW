using LogiMarket.Domain.Catalog.Composite;

namespace LogiMarket.Domain.Catalog.Visitor;

// Reporte 2: peso total estimado para logística.
public class PesoLogisticoVisitor : IVisitorCatalogo
{
    public decimal PesoTotalKg { get; private set; }

    public void Visitar(ProductoSimple producto, int cantidad)
    {
        PesoTotalKg += producto.PesoKg * cantidad;
    }

    public void Visitar(KitCompuesto kit, int cantidad)
    {
    }
}