using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.ValueObjects;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Patterns.Creational;

public class BuilderTests
{
    private static Cliente Ana() => new("Ana", CategoriaCliente.Vip);

    [Fact]
    public void Construir_SinCliente_LanzaDomainException()
    {
        var builder = new PedidoBuilder().AgregarLinea(P01(), 1);

        Assert.Throws<DomainException>(() => builder.Construir());
    }

    [Fact]
    public void Construir_SinLineas_LanzaDomainException()
    {
        var builder = new PedidoBuilder().ConCliente(Ana());

        Assert.Throws<DomainException>(() => builder.Construir());
    }

    [Fact]
    public void Construir_CantidadCero_LanzaDomainException()
    {
        var builder = new PedidoBuilder().ConCliente(Ana()).AgregarLinea(P01(), 0);

        Assert.Throws<DomainException>(() => builder.Construir());
    }

    [Fact]
    public void Construir_PedidoDeLuisConVariasLineas_QuedaCompletoYValido()
    {
        var luis = new Cliente("Luis", CategoriaCliente.Normal);

        var pedido = new PedidoBuilder()
            .ConCliente(luis)
            .ConEntrega(TipoEntrega.Expres)
            .AgregarLinea(P04(), 2)
            .AgregarLinea(P05(), 2)
            .Construir();

        Assert.Equal(luis.Id, pedido.ClienteId);
        Assert.Equal(2, pedido.Detalles.Count);
        Assert.Equal(90m, pedido.Subtotal.Valor); // 2 x 40 + 2 x 5
        Assert.Equal(4, pedido.UnidadesFisicas);
        Assert.Equal(TipoEntrega.Expres, pedido.Entrega);
        Assert.Equal(EstadoPedido.Borrador, pedido.Estado);
    }

    [Fact]
    public void Construir_ConKit_GuardaInstantaneaDeLaComposicion()
    {
        var pedido = new PedidoBuilder()
            .ConCliente(Ana())
            .AgregarLinea(CrearKitTeletrabajo(), 1)
            .Construir();

        var linea = Assert.Single(pedido.Detalles);
        Assert.Equal("KIT_TELETRABAJO", linea.Sku);
        Assert.Equal(60m, linea.PrecioUnitario.Valor);
        Assert.Equal(3, linea.Componentes.Count);
        Assert.Contains(new ComponenteFisico("P01", 1), linea.Componentes);
        Assert.Contains(new ComponenteFisico("P02", 1), linea.Componentes);
        Assert.Contains(new ComponenteFisico("P03", 1), linea.Componentes);
        Assert.Equal(3, pedido.UnidadesFisicas);
    }

    [Fact]
    public void Construir_CambioDePrecioPosterior_NoAlteraElPedido()
    {
        var producto = new Producto("P01", "Mouse USB", new Dinero(20m), 0.20m, 8);
        var pedido = new PedidoBuilder()
            .ConCliente(Ana())
            .AgregarLinea(new ProductoSimple(producto), 1)
            .Construir();

        producto.CambiarPrecio(new Dinero(99m)); // el catálogo cambia después

        Assert.Equal(20m, pedido.Detalles[0].PrecioUnitario.Valor);
        Assert.Equal(20m, pedido.Subtotal.Valor);
    }

    [Fact]
    public void Construir_MismoSkuDosVeces_SumaLasCantidades()
    {
        var pedido = new PedidoBuilder()
            .ConCliente(Ana())
            .AgregarLinea(P01(), 1)
            .AgregarLinea(P01(), 2)
            .Construir();

        var linea = Assert.Single(pedido.Detalles);
        Assert.Equal(3, linea.Cantidad);
    }
}