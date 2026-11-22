using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Returns.TemplateMethod;
using LogiMarket.Tests.Fakes;

namespace LogiMarket.Tests.Patterns.Behavioral;

public class TemplateMethodTests
{
    // Solicitud hecha "dias" después de la entrega del pedido.
    private static SolicitudDevolucion Solicitud(Pedido pedido, int dias, bool sinUso = true) =>
        new(pedido, pedido.FechaEntregaUtc!.Value.AddDays(dias), sinUso);

    [Fact]
    public void Defecto_Dia20_SeAceptaConReembolsoSinTarifaYUnReintegro()
    {
        var pedido = PedidosDePrueba.EntregadoDeAna();

        var resultado = new DevolucionPorDefecto().Ejecutar(Solicitud(pedido, 20));

        Assert.True(resultado.Aceptada);
        var devolucion = resultado.Devolucion!;
        Assert.Equal(MotivoDevolucion.Defecto, devolucion.Motivo);
        Assert.Equal(54m, devolucion.Reembolso.Valor);                  // sin los 5.00 de envío
        Assert.NotEqual(pedido.Total.Valor, devolucion.Reembolso.Valor); // el total pagado era 59.00
        Assert.Contains(new ComponenteFisico("P01", 1), devolucion.Reintegros);
        Assert.Contains(new ComponenteFisico("P02", 1), devolucion.Reintegros);
        Assert.Contains(new ComponenteFisico("P03", 1), devolucion.Reintegros);
        Assert.Equal(54m, resultado.Evento!.Reembolso.Valor);
        Assert.True(pedido.Devuelto);
    }

    [Fact]
    public void Arrepentimiento_Dia10_SeRechazaSinReembolsoNiReintegro()
    {
        var pedido = PedidosDePrueba.EntregadoDeAna();

        var resultado = new DevolucionPorArrepentimiento().Ejecutar(Solicitud(pedido, 10));

        Assert.False(resultado.Aceptada);
        Assert.Contains("Plazo vencido", resultado.MotivoRechazo);
        Assert.Null(resultado.Devolucion);
        Assert.Null(resultado.Evento);
        Assert.False(pedido.Devuelto); // sin efectos colaterales
    }

    [Theory]
    [InlineData(true, 30, true)]    // DEFECTO: día 30 todavía entra
    [InlineData(true, 31, false)]   // DEFECTO: día 31 ya no
    [InlineData(false, 7, true)]    // ARREPENTIMIENTO: día 7 todavía entra
    [InlineData(false, 8, false)]   // ARREPENTIMIENTO: día 8 ya no
    public void Plazo_RespetaElLimiteDeCadaVariante(bool esDefecto, int dias, bool aceptadaEsperada)
    {
        var pedido = PedidosDePrueba.EntregadoDeAna();
        ProcesarDevolucion proceso = esDefecto
            ? new DevolucionPorDefecto()
            : new DevolucionPorArrepentimiento();

        var resultado = proceso.Ejecutar(Solicitud(pedido, dias));

        Assert.Equal(aceptadaEsperada, resultado.Aceptada);
    }

    [Fact]
    public void Arrepentimiento_ProductoUsado_SeRechaza()
    {
        var pedido = PedidosDePrueba.EntregadoDeAna();

        var resultado = new DevolucionPorArrepentimiento().Ejecutar(Solicitud(pedido, 3, sinUso: false));

        Assert.False(resultado.Aceptada);
        Assert.Contains("sin uso", resultado.MotivoRechazo);
        Assert.False(pedido.Devuelto);
    }

    [Fact]
    public void Defecto_ProductoUsado_IgualSeAcepta()
    {
        var pedido = PedidosDePrueba.EntregadoDeAna();

        var resultado = new DevolucionPorDefecto().Ejecutar(Solicitud(pedido, 5, sinUso: false));

        Assert.True(resultado.Aceptada);
    }

    [Fact]
    public void SegundaSolicitud_SeRechazaYSoloHayUnaDevolucion()
    {
        var pedido = PedidosDePrueba.EntregadoDeAna();
        var proceso = new DevolucionPorDefecto();

        var primera = proceso.Ejecutar(Solicitud(pedido, 5));
        var segunda = proceso.Ejecutar(Solicitud(pedido, 6));

        Assert.True(primera.Aceptada);
        Assert.False(segunda.Aceptada);
        Assert.Contains("devuelto", segunda.MotivoRechazo);
        Assert.Null(segunda.Devolucion); // no hay segundo reintegro ni segundo reembolso
    }

    [Fact]
    public void PedidoNoEntregado_SeRechaza()
    {
        var pedido = PedidosDePrueba.Borrador();
        var solicitud = new SolicitudDevolucion(pedido, DateTime.UtcNow, true);

        var resultado = new DevolucionPorDefecto().Ejecutar(solicitud);

        Assert.False(resultado.Aceptada);
        Assert.Contains("ENTREGADOS", resultado.MotivoRechazo);
    }
}