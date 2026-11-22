using LogiMarket.Application.UseCases.ConfirmarPedido.Chain;
using LogiMarket.Application.Validation;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.ValueObjects;
using LogiMarket.Tests.Fakes;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Patterns.Behavioral;

public class ChainTests
{
    private static Cliente Ana() => new("Ana", CategoriaCliente.Vip);

    // Inventario oficial inicial de los productos que usan estos casos.
    private static Dictionary<string, int> Stock() => new() { ["P01"] = 8, ["P02"] = 5, ["P03"] = 20 };

    private static ContextoConfirmacion Contexto(
        Pedido pedido, Cliente? cliente, IReadOnlyDictionary<string, int> stock,
        RolUsuario rol = RolUsuario.Ventas) => new(pedido, cliente, stock, rol);

    // KIT_TELETRABAJO x6: necesita 6 teclados (P02) y solo hay 5.
    private static Pedido PedidoKitX6() =>
        new PedidoBuilder()
            .ConCliente(Ana())
            .AgregarLinea(CrearKitTeletrabajo(), 6)
            .Construir();

    [Fact]
    public void Cadena_PedidoCorrecto_PasaPorLosCuatroEslabonesEnOrden()
    {
        var resultado = CadenaValidacionConfirmacion.Crear()
            .Validar(Contexto(PedidosDePrueba.Borrador(), Ana(), Stock()));

        Assert.True(resultado.Aprobada);
        Assert.Equal(
            new[] { "ValidarClienteActivo", "ValidarLineas", "ValidarStock", "ValidarAutorizacionYCondiciones" },
            resultado.Traza);
    }

    [Fact]
    public void Cadena_SinClienteYSinStock_InformaPrimeroUnoYAlCorregirloElSiguiente()
    {
        var cadena = CadenaValidacionConfirmacion.Crear();
        var pedido = PedidoKitX6();
        var stock = Stock();

        // 1) Sin cliente Y sin stock: solo se informa el PRIMER problema y la cadena se detiene.
        var primero = cadena.Validar(Contexto(pedido, null, stock));
        Assert.False(primero.Aprobada);
        Assert.Equal("CLIENTE_NO_ENCONTRADO", primero.Error!.Codigo);
        Assert.Equal(new[] { "ValidarClienteActivo" }, primero.Traza);

        // 2) Se corrige el cliente: se alcanza el siguiente problema (stock).
        var segundo = cadena.Validar(Contexto(pedido, Ana(), stock));
        var error = segundo.Error!;
        Assert.Equal("STOCK_INSUFICIENTE", error.Codigo);
        Assert.Contains("P02", error.Mensaje);
        Assert.Equal(new[] { "ValidarClienteActivo", "ValidarLineas", "ValidarStock" }, segundo.Traza);

        // 3) Se corrige el stock: la cadena completa aprueba.
        stock["P02"] = 6;
        var tercero = cadena.Validar(Contexto(pedido, Ana(), stock));
        Assert.True(tercero.Aprobada);

        // Validar no cobra ni modifica nada.
        Assert.Equal(EstadoPedido.Borrador, pedido.Estado);
    }

    [Fact]
    public void Cadena_ClienteInactivo_SeRechazaEnElPrimerEslabon()
    {
        var cliente = Ana();
        cliente.Desactivar();

        var resultado = CadenaValidacionConfirmacion.Crear()
            .Validar(Contexto(PedidosDePrueba.Borrador(), cliente, Stock()));

        Assert.Equal("CLIENTE_INACTIVO", resultado.Error!.Codigo);
        Assert.Single(resultado.Traza);
    }

    [Fact]
    public void Cadena_ProductoQueYaNoExisteEnElCatalogo_SeRechazaEnElSegundoEslabon()
    {
        var stock = Stock();
        stock.Remove("P03"); // el kit necesita P03

        var resultado = CadenaValidacionConfirmacion.Crear()
            .Validar(Contexto(PedidosDePrueba.Borrador(), Ana(), stock));

        Assert.Equal("PRODUCTO_NO_DISPONIBLE", resultado.Error!.Codigo);
        Assert.Equal(2, resultado.Traza.Count);
    }

    [Theory]
    [InlineData(RolUsuario.Admin, true)]
    [InlineData(RolUsuario.Ventas, true)]
    [InlineData(RolUsuario.Bodega, false)]
    [InlineData(RolUsuario.Consulta, false)]
    public void Cadena_SoloAdminYVentasPuedenConfirmar(RolUsuario rol, bool aprobadaEsperada)
    {
        var resultado = CadenaValidacionConfirmacion.Crear()
            .Validar(Contexto(PedidosDePrueba.Borrador(), Ana(), Stock(), rol));

        Assert.Equal(aprobadaEsperada, resultado.Aprobada);
        if (!aprobadaEsperada)
            Assert.Equal("NO_AUTORIZADO", resultado.Error!.Codigo);
    }

    [Fact]
    public void Cadena_PedidoYaPagado_NoSePuedeConfirmarOtraVez()
    {
        var pedido = PedidosDePrueba.Borrador();
        pedido.Validar();
        pedido.Valorar(new Dinero(6m), new Dinero(5m));
        pedido.Pagar();

        var resultado = CadenaValidacionConfirmacion.Crear()
            .Validar(Contexto(pedido, Ana(), Stock()));

        Assert.Equal("ESTADO_INVALIDO", resultado.Error!.Codigo);
    }

    // Eslabón inventado SOLO para la prueba: demuestra que se puede agregar uno nuevo
    // sin editar ninguno de los existentes.
    private sealed class EslabonLimiteUnidades : EslabonValidacion
    {
        protected override ErrorValidacion? Evaluar(ContextoConfirmacion contexto) =>
            contexto.Pedido.UnidadesFisicas > 2
                ? new("LIMITE_UNIDADES", "El pedido supera el límite de unidades.")
                : null;
    }

    [Fact]
    public void Cadena_NuevoEslabon_SeAgregaSinEditarLosExistentes()
    {
        var cadena = new ValidarClienteActivo();
        cadena.Siguiente(new EslabonLimiteUnidades());

        var resultado = cadena.Validar(Contexto(PedidosDePrueba.Borrador(), Ana(), Stock())); // 3 unidades

        Assert.Equal("LIMITE_UNIDADES", resultado.Error!.Codigo);
        Assert.Equal(new[] { "ValidarClienteActivo", "EslabonLimiteUnidades" }, resultado.Traza);
    }
}