using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Tests.Domain;

public class DineroTests
{
    // [Fact] marca un método como prueba. xUnit lo ejecuta de forma independiente.
    // Cada prueba sigue el patrón AAA: Arrange (preparar), Act (ejecutar), Assert (verificar).

    [Fact]
    public void Constructor_PuntoMedio_RedondeaAlejandoseDelCero()
    {
        var dinero = new Dinero(0.125m);

        // El redondeo bancario de .NET daría 0.12; la guía exige AwayFromZero.
        Assert.Equal(0.13m, dinero.Valor);
    }

    [Fact]
    public void Multiplicar_PorEntero_DevuelveSubtotal()
    {
        var precio = new Dinero(20m);

        var subtotal = precio * 3;

        Assert.Equal(60m, subtotal.Valor);
    }

    [Fact]
    public void Multiplicar_PorTasa_CalculaDescuento()
    {
        var subtotal = new Dinero(60m);

        var descuento = subtotal * 0.10m; // VIP

        Assert.Equal(6m, descuento.Valor);
    }

    [Fact]
    public void Operaciones_CasoAnaVip_TotalEs59()
    {
        // Caso oficial de la guía: 60.00 - 6.00 + 5.00 = 59.00
        var subtotal = new Dinero(60m);
        var descuento = subtotal * 0.10m;
        var tarifa = new Dinero(5m);

        var total = subtotal - descuento + tarifa;

        Assert.Equal(59m, total.Valor);
    }

    [Fact]
    public void ToString_MuestraDosDecimales()
    {
        var dinero = new Dinero(59m);

        Assert.Equal("59.00", dinero.ToString());
    }
}