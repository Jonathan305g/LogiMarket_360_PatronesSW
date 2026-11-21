using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Tests.Patterns.Structural;

public class CompositeTests
{
    // Productos oficiales de la guía.
    private static ProductoSimple P01() => new(new Producto("P01", "Mouse USB", new Dinero(20m), 0.20m, 8));
    private static ProductoSimple P02() => new(new Producto("P02", "Teclado", new Dinero(30m), 0.70m, 5));
    private static ProductoSimple P03() => new(new Producto("P03", "Memoria USB", new Dinero(10m), 0.10m, 20));

    // KIT_OFICINA = P01 + P02
    private static KitCompuesto CrearKitOficina()
    {
        var kit = new KitCompuesto("KIT_OFICINA", "Kit Oficina");
        kit.Agregar(P01());
        kit.Agregar(P02());
        return kit;
    }

    // KIT_TELETRABAJO = KIT_OFICINA + P03 (un kit dentro de otro kit)
    private static KitCompuesto CrearKitTeletrabajo()
    {
        var kit = new KitCompuesto("KIT_TELETRABAJO", "Kit Teletrabajo");
        kit.Agregar(CrearKitOficina());
        kit.Agregar(P03());
        return kit;
    }

    [Fact]
    public void ObtenerPrecio_KitOficina_Es50()
    {
        Assert.Equal(50m, CrearKitOficina().ObtenerPrecio().Valor);
    }

    [Fact]
    public void ObtenerPrecio_KitTeletrabajoAnidado_Es60()
    {
        Assert.Equal(60m, CrearKitTeletrabajo().ObtenerPrecio().Valor);
    }

    [Fact]
    public void ExpandirComponentes_KitTeletrabajo_DevuelveSkuFinalesConCantidad()
    {
        var componentes = CrearKitTeletrabajo().ExpandirComponentes().ToList();

        // Solo SKU finales (ningún kit) y una unidad de cada uno.
        Assert.Equal(3, componentes.Count);
        Assert.Contains(new ComponenteFisico("P01", 1), componentes);
        Assert.Contains(new ComponenteFisico("P02", 1), componentes);
        Assert.Contains(new ComponenteFisico("P03", 1), componentes);
    }

    [Fact]
    public void ExpandirComponentes_MultiplicaCantidadesPorNivel()
    {
        // Un kit con 2 unidades de KIT_OFICINA -> 2 mouse y 2 teclados.
        var doble = new KitCompuesto("KIT_DOBLE", "Kit Doble");
        doble.Agregar(CrearKitOficina(), 2);

        var componentes = doble.ExpandirComponentes().ToList();

        Assert.Contains(new ComponenteFisico("P01", 2), componentes);
        Assert.Contains(new ComponenteFisico("P02", 2), componentes);
    }

    [Fact]
    public void Agregar_KitDentroDeSiMismo_LanzaDomainException()
    {
        var kit = CrearKitOficina();

        Assert.Throws<DomainException>(() => kit.Agregar(kit));
    }

    [Fact]
    public void Agregar_CicloIndirecto_LanzaDomainException()
    {
        var oficina = CrearKitOficina();
        var teletrabajo = new KitCompuesto("KIT_TELETRABAJO", "Kit Teletrabajo");
        teletrabajo.Agregar(oficina); // teletrabajo contiene a oficina

        // oficina contener a teletrabajo cerraría el círculo.
        Assert.Throws<DomainException>(() => oficina.Agregar(teletrabajo));
    }
}