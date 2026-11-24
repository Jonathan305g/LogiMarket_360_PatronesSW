namespace LogiMarket.Application.Comprobantes.Bridge;

// Implementor del Bridge: entrega el comprobante mediante un formato o destino.
public interface ISalidaDocumento
{
    string Entregar(string titulo, IReadOnlyList<string> lineas);
}
