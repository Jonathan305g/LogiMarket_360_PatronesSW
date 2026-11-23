using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Commands.Command;
using LogiMarket.Application.UseCases.ConfirmarPedido.Facade;
using LogiMarket.Application.UseCases.Despacho;
using LogiMarket.Application.UseCases.Devoluciones;
using LogiMarket.Application.Validation;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Tests.Fakes;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Patterns.Behavioral;

public class CommandTests
{
    // Pedido de Ana (VIP): KIT_TELETRABAJO x1, ESTÁNDAR, ya sembrado en el almacén.
    private static (Escenario E, Pedido Pedido) AnaConKit(RolUsuario rol = RolUsuario.Ventas)
    {
        var e = new Escenario(rol);
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(
            Escenario.PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 1)));
        return (e, pedido);
    }

    private static ConfirmarPedidoCommand Confirmar(Escenario e, Pedido p, string clave) =>
        new(e.Facade, new ConfirmarPedidoSolicitud(p.Id, clave));

    [Fact]
    public async Task Confirmar_MismoComandoDesdeDosInvocadores_DaElMismoResultadoFuncional()   // P24
    {
        var (escenarioUi, pedidoUi) = AnaConKit();
        var (escenarioPrueba, pedidoPrueba) = AnaConKit();

        // "Botón de la UI": un invocador.
        var desdeUi = await escenarioUi.Invocador.EjecutarAsync(Confirmar(escenarioUi, pedidoUi, "K-UI"));

        // "Prueba": OTRO invocador ejecutando el MISMO tipo de comando.
        var invocadorDePrueba = new InvocadorComandos(escenarioPrueba.Usuario);
        var desdePrueba = await invocadorDePrueba.EjecutarAsync(Confirmar(escenarioPrueba, pedidoPrueba, "K-TEST"));

        Assert.True(desdeUi.Exitoso);
        Assert.True(desdePrueba.Exitoso);
        Assert.Equal(desdeUi.Pago!.Monto, desdePrueba.Pago!.Monto);          // mismo importe: 59.00
        Assert.Equal(escenarioUi.Stock("P01"), escenarioPrueba.Stock("P01")); // mismo stock resultante
        Assert.Equal(pedidoUi.Estado, pedidoPrueba.Estado);
        Assert.Single(escenarioUi.Invocador.Registro);                         // y ambos quedaron registrados
        Assert.Single(invocadorDePrueba.Registro);
    }

    [Fact]
    public async Task Invocador_RegistraComandoEntradaUsuarioYResultado()
    {
        var (e, pedido) = AnaConKit();

        await e.Invocador.EjecutarAsync(Confirmar(e, pedido, "K-ANA"));

        var registro = Assert.Single(e.Invocador.Registro);
        Assert.Equal("ConfirmarPedido", registro.Comando);
        Assert.Contains(pedido.Id.ToString(), registro.Entrada);
        Assert.Contains("K-ANA", registro.Entrada);
        Assert.Equal("tester", registro.Usuario);
        Assert.True(registro.Exitoso);
        Assert.StartsWith("Confirmado", registro.Resultado);
    }

    [Fact]
    public async Task Invocador_UnRechazoDeNegocioTambienSeRegistra()
    {
        var e = new Escenario();
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(
            Escenario.PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 6))); // pide 6 teclados, hay 5

        var resultado = await e.Invocador.EjecutarAsync(Confirmar(e, pedido, "K-1"));

        Assert.False(resultado.Exitoso);
        var registro = Assert.Single(e.Invocador.Registro);
        Assert.False(registro.Exitoso);
        Assert.StartsWith("STOCK_INSUFICIENTE", registro.Resultado);
    }

    [Fact]
    public async Task Invocador_UnaExcepcionSeRegistraYSeVuelveALanzar()
    {
        var (e, pedido) = AnaConKit();
        e.Pasarela.FalloTecnico = new InvalidOperationException("Pasarela caída");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Invocador.EjecutarAsync(Confirmar(e, pedido, "K-1")));

        var registro = Assert.Single(e.Invocador.Registro);
        Assert.False(registro.Exitoso);
        Assert.Contains("InvalidOperationException", registro.Resultado);
    }

    [Fact]
    public async Task Invocador_VariosComandos_QuedanRegistradosEnOrden()
    {
        var (e, pedido) = AnaConKit();
        var otro = e.Almacen.Sembrar(
            Escenario.PedidoDe(e.Almacen.Sembrar(new Cliente("Luis", CategoriaCliente.Normal)),
                TipoEntrega.Estandar, (P01(), 1)));

        await e.Invocador.EjecutarAsync(Confirmar(e, pedido, "K-1"));
        await e.Invocador.EjecutarAsync(new CancelarPedidoCommand(e.Cancelacion, otro.Id));

        Assert.Equal(
            new[] { "ConfirmarPedido", "CancelarPedido" },
            e.Invocador.Registro.Select(r => r.Comando));
    }

    [Fact]
    public async Task Cancelar_PedidoEnBorrador_QuedaCancelado()
    {
        var (e, pedido) = AnaConKit();

        var resultado = await e.Invocador.EjecutarAsync(new CancelarPedidoCommand(e.Cancelacion, pedido.Id));

        Assert.True(resultado.Exitoso);
        Assert.Equal(EstadoPedido.Cancelado, pedido.Estado);
        Assert.Equal(1, e.Almacen.Commits);
    }

    [Fact]
    public async Task Cancelar_PedidoYaPagado_SeDeniegaYLosDatosQuedanIntactos()   // P08
    {
        var (e, pedido) = AnaConKit();
        await e.Invocador.EjecutarAsync(Confirmar(e, pedido, "K-1"));
        var commitsAntes = e.Almacen.Commits;

        var resultado = await e.Invocador.EjecutarAsync(new CancelarPedidoCommand(e.Cancelacion, pedido.Id));

        Assert.False(resultado.Exitoso);
        Assert.Equal("TRANSICION_INVALIDA", resultado.Error!.Codigo);
        Assert.Equal(EstadoPedido.Pagado, pedido.Estado);
        Assert.Equal(commitsAntes, e.Almacen.Commits); // no se guardó nada nuevo
        Assert.Equal(7, e.Stock("P01"));               // el stock sigue descontado
        Assert.Single(e.Almacen.Pagos);
        Assert.False(e.Invocador.Registro[^1].Exitoso);
    }

    [Fact]
    public async Task Cancelar_RolConsulta_SeDeniegaEnElCasoDeUso()
    {
        var (e, pedido) = AnaConKit(RolUsuario.Consulta);

        var resultado = await e.Invocador.EjecutarAsync(new CancelarPedidoCommand(e.Cancelacion, pedido.Id));

        Assert.Equal("NO_AUTORIZADO", resultado.Error!.Codigo);
        Assert.Equal(EstadoPedido.Borrador, pedido.Estado);
    }

    // Receptor de prueba: demuestra que el comando solo DELEGA y depende de una interfaz.
    private sealed class ConfirmacionEspia : IConfirmacionPedido
    {
        public ConfirmarPedidoSolicitud? Recibida { get; private set; }

        public Task<ResultadoConfirmacion> ConfirmarAsync(ConfirmarPedidoSolicitud solicitud, CancellationToken ct = default)
        {
            Recibida = solicitud;
            return Task.FromResult(ResultadoConfirmacion.Rechazado(new ErrorValidacion("FALSO", "respuesta de prueba")));
        }
    }

    [Fact]
    public async Task Comando_SoloDelegaEnSuReceptor()
    {
        var espia = new ConfirmacionEspia();
        var solicitud = new ConfirmarPedidoSolicitud(Guid.NewGuid(), "K-X");
        var invocador = new InvocadorComandos(new UsuarioActualFalso(RolUsuario.Ventas));

        var resultado = await invocador.EjecutarAsync(new ConfirmarPedidoCommand(espia, solicitud));

        Assert.Same(solicitud, espia.Recibida);                       // el receptor recibió la solicitud intacta
        Assert.Equal("FALSO", resultado.Error!.Codigo);               // y su respuesta pasó sin cambios
        Assert.Equal("FALSO: respuesta de prueba", Assert.Single(invocador.Registro).Resultado);
    }
    [Fact]
    public async Task DespacharPedidoCommand_SeRegistraConLaGuiaEnElResultado()
    {
        var e = new Escenario();
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(Escenario.PedidoDe(ana, TipoEntrega.Estandar, (CrearKitTeletrabajo(), 1)));
        await e.Confirmar(pedido, "K-1");
        var gestion = new GestionDespachoCasoUso(e.Almacen, e.Almacen, e.Almacen, e.Almacen, e.Planificador,
            new UsuarioActualFalso(RolUsuario.Bodega));
        var invocadorBodega = new InvocadorComandos(new UsuarioActualFalso(RolUsuario.Bodega, "jefe de bodega"));

        var resultado = await invocadorBodega.EjecutarAsync(new DespacharPedidoCommand(gestion, pedido.Id));

        Assert.True(resultado.Exitoso);
        var registro = Assert.Single(invocadorBodega.Registro);
        Assert.Equal("DespacharPedido", registro.Comando);
        Assert.Contains("guía", registro.Resultado, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarDevolucionCommand_UnRechazoDeNegocioTambienSeRegistra()
    {
        var e = new Escenario();
        var ana = e.Almacen.Sembrar(new Cliente("Ana", CategoriaCliente.Vip));
        var pedido = e.Almacen.Sembrar(Escenario.PedidoDe(ana, TipoEntrega.Estandar, (P01(), 1))); // sigue en BORRADOR
        var proceso = new ProcesoDevolucionCasoUso(e.Almacen, e.Almacen, e.Almacen, e.Almacen, e.Almacen, e.Almacen,
            new UsuarioActualFalso(RolUsuario.Ventas));
        var comando = new ProcesarDevolucionCommand(
            proceso, new SolicitudDevolucionDto(pedido.Id, MotivoDevolucion.Defecto, DateTime.UtcNow, false));

        var resultado = await e.Invocador.EjecutarAsync(comando);

        Assert.False(resultado.Exitoso);
        var registro = Assert.Single(e.Invocador.Registro);
        Assert.Equal("ProcesarDevolucion", registro.Comando);
        Assert.False(registro.Exitoso);
    }
}