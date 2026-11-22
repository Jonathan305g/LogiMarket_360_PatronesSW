using LogiMarket.Domain.Exceptions;
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
}