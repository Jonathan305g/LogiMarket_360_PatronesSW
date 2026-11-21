using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Tests.Domain;

public class ProductoTests
{
    // Método auxiliar: crea el Mouse oficial de la guía (P01, 20.00, stock 8).
    // Evita repetir la misma construcción en cada prueba (DRY).
    private static Producto CrearMouse() =>
        new Producto("P01", "Mouse USB", new Dinero(20m), 0.20m, 8);

    [Fact]
    public void Constructor_DatosValidos_CreaProducto()
    {
        var p = CrearMouse();

        Assert.Equal("P01", p.Sku);
        Assert.Equal(8, p.Stock);
        Assert.NotEqual(Guid.Empty, p.Id);
    }

    [Fact]
    public void Constructor_SkuVacio_LanzaDomainException()
    {
        // Assert.Throws verifica que el código lanza ESA excepción; si no la lanza, la prueba falla.
        Assert.Throws<DomainException>(() =>
            new Producto("  ", "Mouse USB", new Dinero(20m), 0.20m, 8));
    }

    [Fact]
    public void Constructor_PrecioCero_LanzaDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Producto("P01", "Mouse USB", new Dinero(0m), 0.20m, 8));
    }

    [Fact]
    public void Constructor_StockNegativo_LanzaDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Producto("P01", "Mouse USB", new Dinero(20m), 0.20m, -1));
    }

    [Fact]
    public void DescontarStock_CantidadValida_ReduceElStock()
    {
        var p = CrearMouse();

        p.DescontarStock(3);

        Assert.Equal(5, p.Stock);
    }

    [Fact]
    public void DescontarStock_MayorAlStock_LanzaYNoCambiaElStock()
    {
        var p = CrearMouse();

        Assert.Throws<DomainException>(() => p.DescontarStock(9));

        // En los rechazos se verifica además que NO hubo efectos colaterales.
        Assert.Equal(8, p.Stock);
    }

    [Fact]
    public void ReintegrarStock_CantidadValida_AumentaElStock()
    {
        var p = CrearMouse();

        p.ReintegrarStock(2);

        Assert.Equal(10, p.Stock);
    }
}