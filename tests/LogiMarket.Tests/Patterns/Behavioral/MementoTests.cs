using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Orders.Memento;
using LogiMarket.Tests.Fakes;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Patterns.Behavioral;

public class MementoTests
{
    private const string Kit = "KIT_TELETRABAJO";

    private static int Cantidad(Pedido pedido, string sku) =>
        pedido.Detalles.Single(d => d.Sku == sku).Cantidad;

    [Fact]
    public void TresEdiciones_DosDeshacer_UnRehacer_RestauraBorradoresCompletos()
    {
        var pedido = PedidosDePrueba.Borrador();   // S0: kit x1
        var historial = new HistorialEdiciones();

        historial.Guardar(pedido); pedido.CambiarCantidad(Kit, 2);   // S1: kit x2
        historial.Guardar(pedido); pedido.AgregarLinea(P04(), 1);    // S2: kit x2 + P04 x1
        historial.Guardar(pedido); pedido.CambiarCantidad(Kit, 3);   // S3: kit x3 + P04 x1

        historial.Deshacer(pedido);   // vuelve a S2
        Assert.Equal(2, pedido.Detalles.Count);
        Assert.Equal(2, Cantidad(pedido, Kit));
        Assert.Equal(1, Cantidad(pedido, "P04"));

        historial.Deshacer(pedido);   // vuelve a S1
        Assert.Single(pedido.Detalles);
        Assert.Equal(2, Cantidad(pedido, Kit));

        historial.Rehacer(pedido);    // avanza otra vez a S2
        Assert.Equal(2, pedido.Detalles.Count);
        Assert.Equal(2, Cantidad(pedido, Kit));
        Assert.Equal(1, Cantidad(pedido, "P04"));
        Assert.Equal(160m, pedido.Subtotal.Valor); // 2 x 60 + 40
    }

    [Fact]
    public void Deshacer_RestauraValoresExactosIncluyendoComponentesDelKit()
    {
        var pedido = PedidosDePrueba.Borrador();
        var historial = new HistorialEdiciones();

        historial.Guardar(pedido);
        pedido.CambiarCantidad(Kit, 5);
        historial.Deshacer(pedido);

        var linea = Assert.Single(pedido.Detalles);
        Assert.Equal(1, linea.Cantidad);
        Assert.Equal(60m, linea.PrecioUnitario.Valor);
        Assert.Equal(3, linea.Componentes.Count); // P01, P02, P03
        Assert.Equal(60m, pedido.Subtotal.Valor);
    }

    [Fact]
    public void Deshacer_RestauraLaFormaDeEntrega()
    {
        var pedido = PedidosDePrueba.Borrador(); // ESTÁNDAR
        var historial = new HistorialEdiciones();

        historial.Guardar(pedido);
        pedido.CambiarEntrega(TipoEntrega.Expres);
        historial.Deshacer(pedido);

        Assert.Equal(TipoEntrega.Estandar, pedido.Entrega);
    }

    [Fact]
    public void Guardar_MasDeCincoEdiciones_ConservaSoloLasCincoUltimas()
    {
        var pedido = PedidosDePrueba.Borrador(); // kit x1
        var historial = new HistorialEdiciones();

        // 7 ediciones: la cantidad del kit pasa de 1 a 8.
        for (var i = 1; i <= 7; i++)
        {
            historial.Guardar(pedido);
            pedido.CambiarCantidad(Kit, i + 1);
        }

        // Solo se pueden deshacer 5 veces.
        for (var i = 0; i < HistorialEdiciones.Capacidad; i++)
            historial.Deshacer(pedido);

        Assert.False(historial.PuedeDeshacer);
        Assert.Equal(3, Cantidad(pedido, Kit)); // las dos primeras ediciones ya se perdieron
        Assert.Throws<DomainException>(() => historial.Deshacer(pedido));
    }

    [Fact]
    public void EdicionNuevaDespuesDeDeshacer_InvalidaElRehacer()
    {
        var pedido = PedidosDePrueba.Borrador();
        var historial = new HistorialEdiciones();

        historial.Guardar(pedido);
        pedido.CambiarCantidad(Kit, 2);
        historial.Deshacer(pedido);
        Assert.True(historial.PuedeRehacer);

        historial.Guardar(pedido);
        pedido.CambiarCantidad(Kit, 4);

        Assert.False(historial.PuedeRehacer);
    }

    [Fact]
    public void PedidoConfirmado_NoSePuedeDeshacerNiGuardar()
    {
        var pedido = PedidosDePrueba.Borrador();
        var historial = new HistorialEdiciones();
        historial.Guardar(pedido);
        pedido.CambiarCantidad(Kit, 2);

        pedido.Validar(); // el pedido sale de BORRADOR

        Assert.Throws<DomainException>(() => historial.Deshacer(pedido));
        Assert.Throws<DomainException>(() => historial.Guardar(pedido));
        Assert.Equal(EstadoPedido.Validado, pedido.Estado);
        Assert.Equal(2, Cantidad(pedido, Kit)); // nada se revirtió
    }

    [Fact]
    public void Limpiar_VaciaAmbasPilas()
    {
        var pedido = PedidosDePrueba.Borrador();
        var historial = new HistorialEdiciones();
        historial.Guardar(pedido);
        pedido.CambiarCantidad(Kit, 2);
        historial.Deshacer(pedido);

        historial.Limpiar();

        Assert.False(historial.PuedeDeshacer);
        Assert.False(historial.PuedeRehacer);
    }

    [Fact]
    public void DeshacerYRehacer_SinHistorial_LanzanDomainException()
    {
        var pedido = PedidosDePrueba.Borrador();
        var historial = new HistorialEdiciones();

        Assert.Throws<DomainException>(() => historial.Deshacer(pedido));
        Assert.Throws<DomainException>(() => historial.Rehacer(pedido));
    }
}