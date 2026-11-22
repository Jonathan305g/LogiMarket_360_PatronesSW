using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Tests.Fakes;

// Catálogo oficial de la guía, reutilizable por todas las pruebas.
public static class CatalogoDePrueba
{
    public static ProductoSimple P01() => new(new Producto("P01", "Mouse USB", new Dinero(20m), 0.20m, 8));
    public static ProductoSimple P02() => new(new Producto("P02", "Teclado", new Dinero(30m), 0.70m, 5));
    public static ProductoSimple P03() => new(new Producto("P03", "Memoria USB", new Dinero(10m), 0.10m, 20));

    public static ProductoSimple P04() => new(new Producto("P04", "Webcam", new Dinero(40m), 0.35m, 4));
    public static ProductoSimple P05() => new(new Producto("P05", "Alfombrilla", new Dinero(5m), 0.10m, 30));

    public static KitCompuesto CrearKitOficina()
    {
        var kit = new KitCompuesto("KIT_OFICINA", "Kit Oficina");
        kit.Agregar(P01());
        kit.Agregar(P02());
        return kit;
    }

    public static KitCompuesto CrearKitTeletrabajo()
    {
        var kit = new KitCompuesto("KIT_TELETRABAJO", "Kit Teletrabajo");
        kit.Agregar(CrearKitOficina());
        kit.Agregar(P03());
        return kit;
    }
}