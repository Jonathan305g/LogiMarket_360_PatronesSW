using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Pricing.Strategy;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Tests.Patterns.Behavioral;

public class StrategyTests
{
    private static CalculadoraDescuento CrearCalculadora() =>
        new(new IPoliticaDescuento[] { new SinDescuento(), new Vip10(), new Mayorista15() });

    // Política inventada SOLO para la prueba: demuestra que se puede extender
    // sin editar la calculadora ni las políticas existentes.
    private sealed class PromoRelampago : IPoliticaDescuento
    {
        public string Nombre => "Relampago20";
        public int Prioridad => 30;
        public decimal Tasa => 0.20m;
        public bool Aplica(ContextoDescuento contexto) => true;
    }

    [Fact]
    public void Calcular_AnaVip_Descuento6()
    {
        // KIT_TELETRABAJO x1 = 3 unidades físicas, subtotal 60.00
        var r = CrearCalculadora().Calcular(new Dinero(60m), new ContextoDescuento(CategoriaCliente.Vip, 3));

        Assert.Equal("Vip10", r.Politica);
        Assert.Equal(6m, r.Monto.Valor);
    }

    [Fact]
    public void Calcular_LuisNormal_SinDescuento()
    {
        // P04 x2 + P05 x2 = 4 unidades, subtotal 90.00
        var r = CrearCalculadora().Calcular(new Dinero(90m), new ContextoDescuento(CategoriaCliente.Normal, 4));

        Assert.Equal("SinDescuento", r.Politica);
        Assert.Equal(0m, r.Monto.Valor);
    }

    [Fact]
    public void Calcular_CarmenMayorista_Descuento15()
    {
        // P03 x10 = 10 unidades, subtotal 100.00, cliente NORMAL
        var r = CrearCalculadora().Calcular(new Dinero(100m), new ContextoDescuento(CategoriaCliente.Normal, 10));

        Assert.Equal("Mayorista15", r.Politica);
        Assert.Equal(15m, r.Monto.Valor);
    }

    [Fact]
    public void Calcular_VipConDiezUnidades_GanaMayoristaYNoSeAcumula()
    {
        var r = CrearCalculadora().Calcular(new Dinero(100m), new ContextoDescuento(CategoriaCliente.Vip, 10));

        Assert.Equal("Mayorista15", r.Politica);
        Assert.Equal(15m, r.Monto.Valor); // ni 10 ni 25
    }

    // [Theory] ejecuta la misma prueba con varios datos; cada [InlineData] es un caso.
    [Theory]
    [InlineData(9, "Vip10")]
    [InlineData(10, "Mayorista15")]
    public void Calcular_LimiteDeUnidades_9NoEsMayorista10Si(int unidades, string politicaEsperada)
    {
        var r = CrearCalculadora().Calcular(new Dinero(100m), new ContextoDescuento(CategoriaCliente.Vip, unidades));

        Assert.Equal(politicaEsperada, r.Politica);
    }

    [Fact]
    public void Calcular_NuevaPromocion_SeAgregaSinEditarLaCalculadora()
    {
        var politicas = new IPoliticaDescuento[]
            { new SinDescuento(), new Vip10(), new Mayorista15(), new PromoRelampago() };
        var calculadora = new CalculadoraDescuento(politicas);

        var r = calculadora.Calcular(new Dinero(100m), new ContextoDescuento(CategoriaCliente.Normal, 1));

        Assert.Equal("Relampago20", r.Politica);
        Assert.Equal(20m, r.Monto.Valor);
    }

    [Fact]
    public void Calcular_SinPoliticaAplicable_LanzaDomainException()
    {
        var calculadora = new CalculadoraDescuento(new IPoliticaDescuento[] { new Vip10() });

        Assert.Throws<DomainException>(() =>
            calculadora.Calcular(new Dinero(50m), new ContextoDescuento(CategoriaCliente.Normal, 1)));
    }
}