using LogiMarket.Domain.Delivery.AbstractFactory;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Orders.Builder;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Patterns.Creational;

public class AbstractFactoryTests
{
    private static readonly DateTime Desde = new(2026, 11, 22, 10, 0, 0, DateTimeKind.Utc);

    private static PlanificadorEntrega CrearPlanificador() =>
        new(new IFabricaEntrega[] { new FabricaEntregaEstandar(), new FabricaEntregaExpres() });

    private static Pedido PedidoCon(TipoEntrega tipo) =>
        new PedidoBuilder()
            .ConCliente(new Cliente("Ana", CategoriaCliente.Vip))
            .ConEntrega(tipo)
            .AgregarLinea(P01(), 1)
            .Construir();

    [Theory]
    [InlineData(TipoEntrega.Estandar, 5, 3, "ESTÁNDAR")]
    [InlineData(TipoEntrega.Expres, 12, 1, "EXPRÉS")]
    public void Planificar_CadaFamilia_DaTarifaPlazoYEtiquetaPropios(
        TipoEntrega tipo, int tarifa, int dias, string marca)
    {
        var plan = CrearPlanificador().Planificar(PedidoCon(tipo), Desde);

        Assert.Equal(tipo, plan.Tipo);
        Assert.Equal((decimal)tarifa, plan.Tarifa.Valor);
        Assert.Equal(dias, plan.DiasEstimados);
        Assert.Equal(Desde.AddDays(dias), plan.FechaEstimadaUtc); // plazo coherente con la familia
        Assert.Contains(marca, plan.Etiqueta);
    }

    [Fact]
    public void Planificar_LasEtiquetasDeCadaFamiliaSonDistinguibles()
    {
        var planificador = CrearPlanificador();

        var estandar = planificador.Planificar(PedidoCon(TipoEntrega.Estandar), Desde);
        var expres = planificador.Planificar(PedidoCon(TipoEntrega.Expres), Desde);

        Assert.DoesNotContain("EXPRÉS", estandar.Etiqueta);
        Assert.DoesNotContain("ESTÁNDAR", expres.Etiqueta);
    }

    [Fact]
    public void Planificar_CambiarLaEntregaDelPedido_NoRequiereTocarElPlanificador()
    {
        var planificador = CrearPlanificador(); // la MISMA instancia para los dos cálculos
        var pedido = PedidoCon(TipoEntrega.Estandar);

        var antes = planificador.Planificar(pedido, Desde);
        pedido.CambiarEntrega(TipoEntrega.Expres);
        var despues = planificador.Planificar(pedido, Desde);

        Assert.Equal(5m, antes.Tarifa.Valor);
        Assert.Equal(12m, despues.Tarifa.Valor);
    }

    [Fact]
    public void Planificar_SinFabricaParaElTipo_LanzaDomainException()
    {
        var soloEstandar = new PlanificadorEntrega(new IFabricaEntrega[] { new FabricaEntregaEstandar() });

        Assert.Throws<DomainException>(() =>
            soloEstandar.Planificar(PedidoCon(TipoEntrega.Expres), Desde));
    }
}