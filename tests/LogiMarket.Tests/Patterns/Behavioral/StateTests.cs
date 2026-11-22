using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Tests.Fakes;

namespace LogiMarket.Tests.Patterns.Behavioral;

public class StateTests
{
    private static Pedido NuevoPedido() => PedidosDePrueba.Borrador();

    // Lleva un pedido a PAGADO por el camino válido.
    private static Pedido PedidoPagado()
    {
        var p = NuevoPedido();
        p.Validar();
        p.Pagar();
        return p;
    }

    [Fact]
    public void CicloCompleto_AvanzaPorTodosLosEstados()
    {
        var p = NuevoPedido();
        Assert.Equal(EstadoPedido.Borrador, p.Estado);

        p.Validar(); Assert.Equal(EstadoPedido.Validado, p.Estado);
        p.Pagar(); Assert.Equal(EstadoPedido.Pagado, p.Estado);
        p.Despachar(); Assert.Equal(EstadoPedido.Despachado, p.Estado);
        p.Entregar(); Assert.Equal(EstadoPedido.Entregado, p.Estado);

        Assert.Equal(4, p.Historial.Count);
    }

    [Fact]
    public void Cancelar_DesdePagado_SeRechazaYNoCambiaNada()
    {
        var p = PedidoPagado();
        var cambiosAntes = p.Historial.Count;

        Assert.Throws<DomainException>(() => p.Cancelar());

        Assert.Equal(EstadoPedido.Pagado, p.Estado);
        Assert.Equal(cambiosAntes, p.Historial.Count);
    }

    [Fact]
    public void Validar_EstadoRepetido_SeRechaza()
    {
        var p = NuevoPedido();
        p.Validar();

        Assert.Throws<DomainException>(() => p.Validar());
        Assert.Equal(EstadoPedido.Validado, p.Estado);
    }

    [Fact]
    public void Despachar_SinPagar_SeRechaza()
    {
        var borrador = NuevoPedido();
        var validado = NuevoPedido();
        validado.Validar();

        Assert.Throws<DomainException>(() => borrador.Despachar());
        Assert.Throws<DomainException>(() => validado.Despachar());
        Assert.Equal(EstadoPedido.Borrador, borrador.Estado);
        Assert.Equal(EstadoPedido.Validado, validado.Estado);
    }

    [Fact]
    public void Cancelar_DesdeBorradorYDesdeValidado_EsValido()
    {
        var borrador = NuevoPedido();
        var validado = NuevoPedido();
        validado.Validar();

        borrador.Cancelar();
        validado.Cancelar();

        Assert.Equal(EstadoPedido.Cancelado, borrador.Estado);
        Assert.Equal(EstadoPedido.Cancelado, validado.Estado);
    }

    [Fact]
    public void Entregado_NoAdmiteMasAcciones()
    {
        var p = PedidoPagado();
        p.Despachar();
        p.Entregar();

        Assert.Throws<DomainException>(() => p.Cancelar());
        Assert.Throws<DomainException>(() => p.Pagar());
        Assert.Equal(EstadoPedido.Entregado, p.Estado);
    }

    [Fact]
    public void Historial_RegistraEstadoAnteriorYNuevo()
    {
        var p = NuevoPedido();

        p.Validar();

        var cambio = Assert.Single(p.Historial);
        Assert.Equal(EstadoPedido.Borrador, cambio.Anterior);
        Assert.Equal(EstadoPedido.Validado, cambio.Nuevo);
    }

}