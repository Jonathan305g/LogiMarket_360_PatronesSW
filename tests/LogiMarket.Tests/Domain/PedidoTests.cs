using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.ValueObjects;
using LogiMarket.Tests.Fakes;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Domain;

public class PedidoTests
{
    [Fact]
    public void CambiarCantidad_EnBorrador_ActualizaElSubtotal()
    {
        var pedido = PedidosDePrueba.Borrador(); // kit x1 = 60.00

        pedido.CambiarCantidad("KIT_TELETRABAJO", 2);

        Assert.Equal(120m, pedido.Subtotal.Valor);
    }

    [Fact]
    public void CambiarCantidad_PedidoValidado_LanzaYNoCambia()
    {
        var pedido = PedidosDePrueba.Borrador();
        pedido.Validar();

        Assert.Throws<DomainException>(() => pedido.CambiarCantidad("KIT_TELETRABAJO", 2));

        Assert.Equal(60m, pedido.Subtotal.Valor);
    }

    [Fact]
    public void AgregarLinea_PedidoValidado_Lanza()
    {
        var pedido = PedidosDePrueba.Borrador();
        pedido.Validar();

        Assert.Throws<DomainException>(() => pedido.AgregarLinea(P01(), 1));
        Assert.Single(pedido.Detalles);
    }

    [Fact]
    public void CambiarCantidad_SkuInexistente_Lanza()
    {
        var pedido = PedidosDePrueba.Borrador();

        Assert.Throws<DomainException>(() => pedido.CambiarCantidad("NO_EXISTE", 2));
    }

    [Fact]
    public void Valorar_PedidoValidado_CalculaElTotalDeAna()
    {
        var pedido = PedidosDePrueba.Borrador();
        pedido.Validar();

        pedido.Valorar(new Dinero(6m), new Dinero(5m));

        Assert.Equal(54m, pedido.SubtotalConDescuento.Valor);
        Assert.Equal(59m, pedido.Total.Valor); // 60 - 6 + 5
    }

    [Fact]
    public void Valorar_PedidoEnBorrador_LanzaDomainException()
    {
        var pedido = PedidosDePrueba.Borrador();

        Assert.Throws<DomainException>(() => pedido.Valorar(new Dinero(6m), new Dinero(5m)));
    }

    [Fact]
    public void Valorar_DescuentoMayorAlSubtotal_LanzaDomainException()
    {
        var pedido = PedidosDePrueba.Borrador();
        pedido.Validar();

        Assert.Throws<DomainException>(() => pedido.Valorar(new Dinero(61m), new Dinero(5m)));
    }

    [Fact]
    public void PedidoPagado_NoCambiaSiCambiaElPrecioDelCatalogoNiAdmiteNuevaValoracion() // P08
    {
        var producto = new Producto("P99", "Mouse de prueba", new Dinero(20m), 0.2m, 8);
        var pedido = new PedidoBuilder()
            .ConCliente(new Cliente("Ana", CategoriaCliente.Vip))
            .AgregarLinea(new ProductoSimple(producto), 1)
            .Construir();
        pedido.Validar();
        pedido.Valorar(Dinero.Cero, new Dinero(5m));
        pedido.Pagar();
        var historialAntes = pedido.Historial.Count;

        producto.CambiarPrecio(new Dinero(99m)); // Una edición del catálogo no reescribe la venta histórica.

        Assert.Throws<DomainException>(() => pedido.Valorar(new Dinero(1m), Dinero.Cero));
        Assert.Equal(20m, pedido.Detalles.Single().PrecioUnitario.Valor);
        Assert.Equal(25m, pedido.Total.Valor);
        Assert.Equal(EstadoPedido.Pagado, pedido.Estado);
        Assert.Equal(historialAntes, pedido.Historial.Count);
    }
}
