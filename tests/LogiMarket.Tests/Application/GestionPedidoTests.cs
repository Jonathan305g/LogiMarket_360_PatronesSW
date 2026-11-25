using LogiMarket.Application.UseCases.Pedidos;
using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Pricing.Strategy;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.Delivery.AbstractFactory;
using LogiMarket.Tests.Fakes;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Application;

public sealed class GestionPedidoTests
{
    private static CotizadorPedidoCasoUso CrearCotizador(Escenario escenario) => new(
        new CalculadoraDescuento(new IPoliticaDescuento[] { new Mayorista15(), new Vip10(), new SinDescuento() }),
        escenario.Planificador);

    [Fact]
    public void Cotizar_AnaVipConKitEstandar_CalculaElImporteOficial()
    {
        var escenario = new Escenario();
        var cliente = new Cliente("Ana", CategoriaCliente.Vip);
        var pedido = new PedidoBuilder()
            .ConCliente(cliente)
            .ConEntrega(TipoEntrega.Estandar)
            .AgregarLinea(CrearKitTeletrabajo(), 1)
            .ConstruirVacio();
        pedido.AgregarLinea(CrearKitTeletrabajo(), 1);

        var cotizacion = CrearCotizador(escenario).Cotizar(pedido, cliente.Categoria);

        Assert.Equal(60m, cotizacion.Subtotal.Valor);
        Assert.Equal(6m, cotizacion.Descuento.Valor);
        Assert.Equal(5m, cotizacion.Tarifa.Valor);
        Assert.Equal(59m, cotizacion.Total.Valor);
        Assert.Equal("Vip10", cotizacion.Politica);
    }

    [Fact]
    public async Task ValidarPedido_ConVentasPersisteUnaCopiaValidadaYDejaIntactoElBorrador()
    {
        var escenario = new Escenario(RolUsuario.Ventas);
        var cliente = escenario.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var borrador = new PedidoBuilder()
            .ConCliente(cliente)
            .ConEntrega(TipoEntrega.Estandar)
            .AgregarLinea(CrearKitTeletrabajo(), 1)
            .Construir();
        var casoUso = new ValidarPedidoCasoUso(
            escenario.Almacen, escenario.Almacen, CrearCotizador(escenario), escenario.Usuario);

        var resultado = await casoUso.EjecutarAsync(borrador, cliente.Categoria);

        Assert.True(resultado.Exitoso);
        Assert.NotNull(resultado.Pedido);
        Assert.NotSame(borrador, resultado.Pedido);
        Assert.Equal(EstadoPedido.Borrador, borrador.Estado);
        Assert.Equal(EstadoPedido.Validado, resultado.Pedido!.Estado);
        Assert.Equal(59m, resultado.Pedido.Total.Valor);
        Assert.Equal(1, escenario.Almacen.Commits);
        Assert.Single(await escenario.Almacen.ListarResumenAsync());
    }

    [Fact]
    public async Task ValidarPedido_ConsultaSeDeniegaSinPersistir()
    {
        var escenario = new Escenario(RolUsuario.Consulta);
        var cliente = new Cliente("Ana", CategoriaCliente.Vip);
        var borrador = new PedidoBuilder().ConCliente(cliente).AgregarLinea(P01(), 1).Construir();
        var casoUso = new ValidarPedidoCasoUso(
            escenario.Almacen, escenario.Almacen, CrearCotizador(escenario), escenario.Usuario);

        var resultado = await casoUso.EjecutarAsync(borrador, cliente.Categoria);

        Assert.False(resultado.Exitoso);
        Assert.Contains("no puede validar", resultado.Error);
        Assert.Equal(EstadoPedido.Borrador, borrador.Estado);
        Assert.Equal(0, escenario.Almacen.Commits);
        Assert.Empty(await escenario.Almacen.ListarResumenAsync());
    }
}
