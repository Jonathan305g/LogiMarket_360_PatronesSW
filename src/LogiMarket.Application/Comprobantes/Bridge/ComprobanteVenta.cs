using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Comprobantes.Bridge;

public class ComprobanteVenta : ComprobanteBase
{
    private readonly Pedido _pedido;

    public ComprobanteVenta(Pedido pedido, ISalidaDocumento salida) : base(salida)
    {
        _pedido = pedido;
    }

    protected override string Titulo => $"Comprobante de venta - Pedido {_pedido.Id}";

    protected override IReadOnlyList<string> ConstruirLineas()
    {
        var lineas = new List<string>();
        foreach (var d in _pedido.Detalles)
            lineas.Add($"{d.Sku,-15} {d.Nombre,-25} x{d.Cantidad,-3} {d.PrecioUnitario}");

        lineas.Add($"Subtotal: {_pedido.Subtotal}");
        lineas.Add($"Descuento: {_pedido.Descuento}");
        lineas.Add($"Tarifa de entrega: {_pedido.Tarifa}");
        lineas.Add($"Total: {_pedido.Total}");
        return lineas;
    }
}