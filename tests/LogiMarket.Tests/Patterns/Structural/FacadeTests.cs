using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.UseCases.ConfirmarPedido.Chain;
using LogiMarket.Application.UseCases.ConfirmarPedido.Facade;
using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Delivery.AbstractFactory;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Events;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.Pricing.Strategy;
using LogiMarket.Tests.Fakes;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Patterns.Structural;

public class FacadeTests
{
    // Todo el "mundo" de una prueba: almacén falso, pasarela espía y el Facade ya armado.
    private sealed class Escenario
    {
        public AlmacenEnMemoria Almacen { get; } = AlmacenEnMemoria.ConCatalogoOficial();
        public PasarelaEspia Pasarela { get; } = new();
        public ConfirmacionPedidoFacade Facade { get; }

        public Escenario(RolUsuario rol = RolUsuario.Ventas)
        {
            Facade = new ConfirmacionPedidoFacade(
                CadenaValidacionConfirmacion.Crear(),
                Almacen,   // IClienteRepository
                Almacen,   // IPedidoRepository
                Almacen,   // IProductoRepository
                Almacen,   // IPagoRepository
                Almacen,   // IMovimientoInventarioRepository
                Almacen,   // IOutbox
                Almacen,   // IUnitOfWork
                new CalculadoraDescuento(new IPoliticaDescuento[] { new SinDescuento(), new Vip10(), new Mayorista15() }),
                new PlanificadorEntrega(new IFabricaEntrega[] { new FabricaEntregaEstandar(), new FabricaEntregaExpres() }),
                Pasarela,
                new UsuarioActualFalso(rol));
        }

        public Task<ResultadoConfirmacion> Confirmar(Pedido pedido, string clave) =>
            Facade.ConfirmarAsync(new ConfirmarPedidoSolicitud(pedido.Id, clave));

        public int Stock(string sku) => Almacen.ProductoPorSku(sku).Stock;
    }

    private static Pedido PedidoDe(Cliente cliente, TipoEntrega entrega, params (IElementoCatalogo Item, int Cantidad)[] lineas)
    {
        var builder = new PedidoBuilder().ConCliente(cliente).ConEntrega(entrega);
        foreach (var (item, cantidad) in lineas)
            builder.AgregarLinea(item, cantidad);
        return builder.Construir();
    }

    // Comprueba que NO quedó nada guardado ni cambiado (para todos los rechazos).
    private static void AssertSinEfectos(Escenario e, Pedido pedido)
    {
        Assert.Equal(EstadoPedido.Borrador, pedido.Estado);
        Assert.Empty(e.Almacen.Pagos);
        Assert.Empty(e.Almacen.Movimientos);
        Assert.Empty(e.Almacen.EventosOutbox);
        Assert.Equal(0, e.Almacen.Commits);
    }

    [Fact]
    public async Task Confirmar_AnaVipConKit_Total59YStockDescontadoPorSku()   // P02
    {
        var e = new Escenario();
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 1)));

        var r = await e.Confirmar(pedido, "K-ANA");

        Assert.True(r.Exitoso);
        Assert.Equal(59m, r.Pago!.Monto.Valor);
        Assert.Equal(59m, pedido.Total.Valor);
        Assert.Equal(EstadoPedido.Pagado, pedido.Estado);
        Assert.Equal(7, e.Stock("P01"));
        Assert.Equal(4, e.Stock("P02"));
        Assert.Equal(19, e.Stock("P03"));
        Assert.Single(e.Almacen.Pagos);
        Assert.Equal(3, e.Almacen.Movimientos.Count);
        Assert.All(e.Almacen.Movimientos, m => Assert.Equal(-1, m.Cantidad));
        Assert.Single(e.Almacen.EventosOutbox.OfType<PedidoPagado>());
        Assert.Equal(59m, Assert.Single(e.Pasarela.Llamadas).Monto);
    }

    [Fact]
    public async Task Confirmar_LuisNormalExpres_Total102()   // P03
    {
        var e = new Escenario();
        var luis = e.Almacen.Sembrar(new Cliente("Luis", CategoriaCliente.Normal));
        var pedido = e.Almacen.Sembrar(PedidoDe(luis, TipoEntrega.Expres, (P04(), 2), (P05(), 2)));

        var r = await e.Confirmar(pedido, "K-LUIS");

        Assert.True(r.Exitoso);
        Assert.Equal(102m, r.Pago!.Monto.Valor); // (80 + 10) + 12
        Assert.Equal(2, e.Stock("P04"));
        Assert.Equal(28, e.Stock("P05"));
        Assert.Single(e.Almacen.Pagos);
    }

    [Fact]
    public async Task Confirmar_CarmenMayorista_Total90ConDescuento15()   // P04
    {
        var e = new Escenario();
        var carmen = e.Almacen.Sembrar(new Cliente("Carmen", CategoriaCliente.Normal));
        var pedido = e.Almacen.Sembrar(PedidoDe(carmen, TipoEntrega.Estandar, (P03(), 10)));

        var r = await e.Confirmar(pedido, "K-CARMEN");

        Assert.True(r.Exitoso);
        Assert.Equal(15m, pedido.Descuento.Valor);
        Assert.Equal(90m, r.Pago!.Monto.Valor);  // 100 - 15 + 5
        Assert.Equal(10, e.Stock("P03"));
    }

    [Fact]
    public async Task Confirmar_KitX6SinStockSuficiente_SeRechazaSinEfectos()   // P05
    {
        var e = new Escenario();
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 6)));

        var r = await e.Confirmar(pedido, "K-1");

        Assert.False(r.Exitoso);
        Assert.Equal("STOCK_INSUFICIENTE", r.Error!.Codigo);
        Assert.Equal(0, e.Pasarela.Invocaciones);   // ni siquiera se intentó cobrar
        Assert.Equal(8, e.Stock("P01"));
        Assert.Equal(5, e.Stock("P02"));
        Assert.Equal(20, e.Stock("P03"));
        AssertSinEfectos(e, pedido);
    }

    [Fact]
    public async Task Confirmar_CobroRechazado_ConservaInventarioYEstadoPrevio()   // P06
    {
        var e = new Escenario();
        e.Pasarela.Respuesta = ResultadoCobro.Rechazo("Fondos insuficientes");
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 1)));

        var r = await e.Confirmar(pedido, "K-1");

        Assert.False(r.Exitoso);
        Assert.Equal("COBRO_RECHAZADO", r.Error!.Codigo);
        Assert.Equal(8, e.Stock("P01"));
        Assert.Equal(5, e.Stock("P02"));
        Assert.Equal(20, e.Stock("P03"));
        AssertSinEfectos(e, pedido); // sin pago aprobado ni Outbox de éxito
    }

    [Fact]
    public async Task Confirmar_MismaClaveDosVeces_UnSoloCobroYUnSoloDescuento()   // P07
    {
        var e = new Escenario();
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 1)));

        var primero = await e.Confirmar(pedido, "K-ANA");
        var segundo = await e.Confirmar(pedido, "K-ANA");

        Assert.False(primero.EsRepeticion);
        Assert.True(segundo.Exitoso);
        Assert.True(segundo.EsRepeticion);
        Assert.Equal(primero.Pago!.Id, segundo.Pago!.Id);   // el resultado original
        Assert.Equal(1, e.Pasarela.Invocaciones);            // no se cobró dos veces
        Assert.Single(e.Almacen.Pagos);
        Assert.Equal(3, e.Almacen.Movimientos.Count);        // un movimiento por SKU, no seis
        Assert.Equal(7, e.Stock("P01"));                     // descontado una sola vez
    }

    [Fact]
    public async Task Confirmar_ClaveDeOtroPedido_SeRechaza()
    {
        var e = new Escenario();
        var luis = e.Almacen.Sembrar(new Cliente("Luis", CategoriaCliente.Normal));
        var pedido1 = e.Almacen.Sembrar(PedidoDe(luis, TipoEntrega.Estandar, (P01(), 1)));
        var pedido2 = e.Almacen.Sembrar(PedidoDe(luis, TipoEntrega.Estandar, (P05(), 1)));

        await e.Confirmar(pedido1, "K-1");
        var r = await e.Confirmar(pedido2, "K-1");

        Assert.False(r.Exitoso);
        Assert.Equal("CLAVE_REUTILIZADA", r.Error!.Codigo);
        Assert.Equal(1, e.Pasarela.Invocaciones);
        Assert.Equal(EstadoPedido.Borrador, pedido2.Estado);
    }

    [Fact]
    public async Task Confirmar_PedidoYaPagadoConOtraClave_NoSeCobraDeNuevo()
    {
        var e = new Escenario();
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 1)));

        await e.Confirmar(pedido, "K-1");
        var r = await e.Confirmar(pedido, "K-2");

        Assert.Equal("ESTADO_INVALIDO", r.Error!.Codigo);
        Assert.Equal(1, e.Pasarela.Invocaciones);
        Assert.Single(e.Almacen.Pagos);
    }

    [Fact]
    public async Task Confirmar_FallaLaPasarela_NoQuedaPersistenciaParcial()   // P20
    {
        var e = new Escenario();
        e.Pasarela.FalloTecnico = new InvalidOperationException("Pasarela caída");
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 1)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Confirmar(pedido, "K-1"));

        Assert.Equal(8, e.Stock("P01"));
        AssertSinEfectos(e, pedido);
    }

    [Fact]
    public async Task Confirmar_FallaElCommit_NadaQuedaGuardado()   // P20
    {
        var e = new Escenario();
        e.Almacen.FalloEnCommit = new ConflictoConcurrenciaException();
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 1)));

        var r = await e.Confirmar(pedido, "K-1");

        Assert.False(r.Exitoso);
        Assert.Equal("CONFLICTO_CONCURRENCIA", r.Error!.Codigo);
        Assert.Empty(e.Almacen.Pagos);
        Assert.Empty(e.Almacen.Movimientos);
        Assert.Empty(e.Almacen.EventosOutbox);
        // Nota: el cobro simulado ya se había ejecutado. Con una pasarela real habría que anularlo
        // (límite que se documenta en el informe).
        Assert.Equal(1, e.Pasarela.Invocaciones);
    }

    [Fact]
    public async Task Confirmar_RolConsulta_SeRechazaAntesDeCobrar()
    {
        var e = new Escenario(RolUsuario.Consulta);
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 1)));

        var r = await e.Confirmar(pedido, "K-1");

        Assert.Equal("NO_AUTORIZADO", r.Error!.Codigo);
        Assert.Equal(0, e.Pasarela.Invocaciones);
        AssertSinEfectos(e, pedido);
    }
}