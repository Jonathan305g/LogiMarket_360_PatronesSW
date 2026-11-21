using LogiMarket.Domain.Catalog.Composite;

namespace LogiMarket.Domain.Catalog.Visitor;

// Una operación por cada tipo de elemento del catálogo.
// "cantidad" ya viene multiplicada por los niveles superiores del kit.
public interface IVisitorCatalogo
{
    void Visitar(ProductoSimple producto, int cantidad);
    void Visitar(KitCompuesto kit, int cantidad);
}