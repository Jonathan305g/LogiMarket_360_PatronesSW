using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Comprobantes.Bridge;

public class ComprobanteDevolucion : ComprobanteBase
{
    private readonly Devolucion _devolucion;

    public ComprobanteDevolucion(Devolucion devolucion, ISalidaDocumento salida) : base(salida)
    {
        _devolucion = devolucion;
    }

    protected override string Titulo => $"Comprobante de devolucion - Pedido {_devolucion.PedidoId}";

    protected override IReadOnlyList<string> ConstruirLineas()
    {
        var lineas = new List<string> { $"Motivo: {_devolucion.Motivo}" };
        foreach (var r in _devolucion.Reintegros)
            lineas.Add($"Reintegro: {r.Sku} x{r.Cantidad}");
        lineas.Add($"Reembolso: {_devolucion.Reembolso}");
        return lineas;
    }
}