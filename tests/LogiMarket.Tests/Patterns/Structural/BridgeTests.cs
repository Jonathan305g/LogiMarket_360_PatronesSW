using LogiMarket.Application.Comprobantes.Bridge;
using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.Returns.TemplateMethod;
using LogiMarket.Domain.ValueObjects;
using LogiMarket.Infrastructure.Comprobantes;
using LogiMarket.Tests.Fakes;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Patterns.Structural;

public class BridgeTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), $"logimarket_comprobantes_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_carpeta)) Directory.Delete(_carpeta, recursive: true);
    }

    private static Pedido CrearPedidoDePrueba()
    {
        var pedido = new PedidoBuilder()
            .ConCliente(new Cliente("Ana", CategoriaCliente.Vip)).ConEntrega(TipoEntrega.Estandar)
            .AgregarLinea(P01(), 2)
            .Construir();
        pedido.Validar();
        pedido.Valorar(new Dinero(0m), new Dinero(5m));
        return pedido;
    }

    [Fact]
    public void ComprobanteVenta_ConVistaPrevia_DevuelveTextoEnMemoria()
    {
        var texto = new ComprobanteVenta(CrearPedidoDePrueba(), new VistaPrevia()).Generar();

        Assert.Contains("Comprobante de venta", texto);
        Assert.Contains("P01", texto);
    }

    [Fact]
    public void ComprobanteVenta_ConArchivoTXT_EscribeUnArchivoYDevuelveSuRuta()
    {
        var ruta = new ComprobanteVenta(CrearPedidoDePrueba(), new ArchivoTXT(_carpeta)).Generar();

        Assert.True(File.Exists(ruta));
        Assert.Contains("Comprobante de venta", File.ReadAllText(ruta));
    }

    [Fact]
    public void ComprobanteDevolucion_ConVistaPreviaYArchivoTXT_EntregaLasDosSalidas()
    {
        var pedido = PedidosDePrueba.EntregadoDeAna();
        var solicitud = new SolicitudDevolucion(pedido, pedido.FechaEntregaUtc!.Value.AddDays(2), true);
        var resultado = new DevolucionPorDefecto().Ejecutar(solicitud);
        Assert.True(resultado.Aceptada);

        var preview = new ComprobanteDevolucion(resultado.Devolucion!, new VistaPrevia()).Generar();
        var ruta = new ComprobanteDevolucion(resultado.Devolucion!, new ArchivoTXT(_carpeta)).Generar();

        Assert.Contains("Comprobante de devolucion", preview);
        Assert.Contains("Reembolso", preview);
        Assert.True(File.Exists(ruta));
        Assert.Contains("Comprobante de devolucion", File.ReadAllText(ruta));
    }

    [Fact]
    public void MismaAbstraccion_DosImplementadoresDistintos_SonIntercambiables()
    {
        var pedido = CrearPedidoDePrueba();
        ISalidaDocumento[] salidas = { new VistaPrevia(), new ArchivoTXT(_carpeta) };

        foreach (var salida in salidas)
            Assert.False(string.IsNullOrWhiteSpace(new ComprobanteVenta(pedido, salida).Generar()));
    }
}
