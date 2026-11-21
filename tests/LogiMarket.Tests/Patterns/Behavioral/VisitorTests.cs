using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Catalog.Visitor;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Patterns.Behavioral;

public class VisitorTests
{
    [Fact]
    public void DesgloseFisico_KitTeletrabajo_EnumeraP01P02P03()
    {
        var visitor = new DesgloseFisicoVisitor();

        CrearKitTeletrabajo().Aceptar(visitor, 1);

        Assert.Equal(3, visitor.Desglose.Count);
        Assert.Equal(1, visitor.Desglose["P01"]);
        Assert.Equal(1, visitor.Desglose["P02"]);
        Assert.Equal(1, visitor.Desglose["P03"]);
    }

    [Fact]
    public void PesoLogistico_KitTeletrabajo_Es1Kg()
    {
        var visitor = new PesoLogisticoVisitor();

        CrearKitTeletrabajo().Aceptar(visitor, 1);

        // 0.20 (mouse) + 0.70 (teclado) + 0.10 (memoria) = 1.00
        Assert.Equal(1.00m, visitor.PesoTotalKg);
    }

    [Fact]
    public void PesoLogistico_DosKitsTeletrabajo_Es2Kg()
    {
        var visitor = new PesoLogisticoVisitor();

        CrearKitTeletrabajo().Aceptar(visitor, 2);

        Assert.Equal(2.00m, visitor.PesoTotalKg);
    }
}