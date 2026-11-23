using LogiMarket.Application.UseCases.Despacho;
using LogiMarket.Application.UseCases.Devoluciones;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Tests.Fakes;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Application;

public class DespachoYDevolucionTests
{
    // Pedido de Ana (kit x1) ya sembrado y confirmado hasta PAGADO.
    private static async Task<(Escenario E, Pedido Pedido)> AnaPagada()
    {
        var e = new Escenario();
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(Escenario.PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 1)));
        await e.Confirmar(pedido, "K-1");
        return (e, pedido);
    }

    private static GestionDespachoCasoUso Despacho(Escenario e, RolUsuario rol) =>
        new(e.Almacen, e.Almacen, e.Almacen, e.Almacen, e.Planificador, new UsuarioActualFalso(rol));

    private static ProcesoDevolucionCasoUso Devoluciones(Escenario e, RolUsuario rol) =>
        new(e.Almacen, e.Almacen, e.Almacen, e.Almacen, e.Almacen, e.Almacen, new UsuarioActualFalso(rol));

    [Fact]
    public async Task Despachar_PedidoPagadoConBodega_GeneraGuiaDeSuFamiliaYCambiaEstado()   // P09
    {
        var (e, pedido) = await AnaPagada();

        var resultado = await Despacho(e, RolUsuario.Bodega).DespacharAsync(pedido.Id);

        Assert.True(resultado.Exitoso);
        Assert.Contains("ESTÁNDAR", resultado.Guia);       // la guía viene de la familia ESTÁNDAR del pedido
        Assert.Equal(EstadoPedido.Despachado, pedido.Estado);
        Assert.Single(e.Almacen.Despachos);
    }

    [Fact]
    public async Task Despachar_SinPago_SeRechazaSinCambios()   // P09
    {
        var e = new Escenario();
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(Escenario.PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 1)));
        pedido.Validar(); // queda VALIDADO, nunca se paga

        var resultado = await Despacho(e, RolUsuario.Bodega).DespacharAsync(pedido.Id);

        Assert.False(resultado.Exitoso);
        Assert.Equal("TRANSICION_INVALIDA", resultado.Error!.Codigo);
        Assert.Equal(EstadoPedido.Validado, pedido.Estado);
        Assert.Empty(e.Almacen.Despachos);
    }

    [Fact]
    public async Task Entregar_DespuesDeDespachar_CompletaLaSecuencia()   // P09
    {
        var (e, pedido) = await AnaPagada();
        var gestion = Despacho(e, RolUsuario.Bodega);
        await gestion.DespacharAsync(pedido.Id);

        var resultado = await gestion.EntregarAsync(pedido.Id);

        Assert.True(resultado.Exitoso);
        Assert.Equal(EstadoPedido.Entregado, pedido.Estado);
        Assert.NotNull(pedido.FechaEntregaUtc);
    }

    [Fact]
    public async Task Despachar_RolConsulta_SeDeniega()
    {
        var (e, pedido) = await AnaPagada();

        var resultado = await Despacho(e, RolUsuario.Consulta).DespacharAsync(pedido.Id);

        Assert.Equal("NO_AUTORIZADO", resultado.Error!.Codigo);
        Assert.Equal(EstadoPedido.Pagado, pedido.Estado);
    }

    // Lleva a Ana hasta ENTREGADO.
    private static async Task<(Escenario E, Pedido Pedido)> AnaEntregada()
    {
        var (e, pedido) = await AnaPagada();
        var gestion = Despacho(e, RolUsuario.Bodega);
        await gestion.DespacharAsync(pedido.Id);
        await gestion.EntregarAsync(pedido.Id);
        return (e, pedido);
    }

    [Fact]
    public async Task Devolucion_DefectoDia20_Reembolsa54YReintegraElStockUnaVez()   // P35
    {
        var (e, pedido) = await AnaEntregada();
        var solicitud = new SolicitudDevolucionDto(
            pedido.Id, MotivoDevolucion.Defecto, pedido.FechaEntregaUtc!.Value.AddDays(20), ProductoSinUso: false);

        var resultado = await Devoluciones(e, RolUsuario.Ventas).SolicitarAsync(solicitud);

        Assert.True(resultado.Exitoso);
        Assert.Equal(54m, resultado.Devolucion!.Reembolso.Valor);   // sin los 5.00 de envío
        Assert.Equal(8, e.Stock("P01"));   // 7 + 1 reintegrado
        Assert.Equal(5, e.Stock("P02"));
        Assert.Equal(20, e.Stock("P03"));
        Assert.Single(e.Almacen.Devoluciones);
        Assert.Equal(3, e.Almacen.Movimientos.Count(m => m.Motivo == MotivoMovimiento.Devolucion));
        Assert.True(pedido.Devuelto);
    }

    [Fact]
    public async Task Devolucion_SegundaSolicitud_SeRechazaYNoDuplicaElReintegro()   // P35
    {
        var (e, pedido) = await AnaEntregada();
        var proceso = Devoluciones(e, RolUsuario.Ventas);
        var fecha = pedido.FechaEntregaUtc!.Value.AddDays(5);

        var primera = await proceso.SolicitarAsync(new SolicitudDevolucionDto(pedido.Id, MotivoDevolucion.Defecto, fecha, false));
        var segunda = await proceso.SolicitarAsync(new SolicitudDevolucionDto(pedido.Id, MotivoDevolucion.Defecto, fecha.AddDays(1), false));

        Assert.True(primera.Exitoso);
        Assert.False(segunda.Exitoso);
        Assert.Equal("DEVOLUCION_RECHAZADA", segunda.Error!.Codigo);
        Assert.Single(e.Almacen.Devoluciones);
        Assert.Equal(8, e.Stock("P01")); // no se reintegró dos veces
    }

    [Fact]
    public async Task Devolucion_ArrepentimientoDia10_SeRechazaSinReembolsoNiReintegro()   // P33
    {
        var (e, pedido) = await AnaEntregada();
        var solicitud = new SolicitudDevolucionDto(
            pedido.Id, MotivoDevolucion.Arrepentimiento, pedido.FechaEntregaUtc!.Value.AddDays(10), ProductoSinUso: true);

        var resultado = await Devoluciones(e, RolUsuario.Ventas).SolicitarAsync(solicitud);

        Assert.False(resultado.Exitoso);
        Assert.Contains("Plazo vencido", resultado.Error!.Mensaje);
        Assert.Equal(7, e.Stock("P01")); // el Facade ya lo había descontado; aquí no se reintegra
        Assert.Empty(e.Almacen.Devoluciones);
        Assert.False(pedido.Devuelto);
    }

    [Fact]
    public async Task Devolucion_RolBodega_SeDeniegaAntesDeTocarElDominio()
    {
        var (e, pedido) = await AnaEntregada();

        var resultado = await Devoluciones(e, RolUsuario.Bodega).SolicitarAsync(
            new SolicitudDevolucionDto(pedido.Id, MotivoDevolucion.Defecto, pedido.FechaEntregaUtc!.Value.AddDays(1), false));

        Assert.Equal("NO_AUTORIZADO", resultado.Error!.Codigo);
        Assert.False(pedido.Devuelto);
    }
}