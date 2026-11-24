namespace LogiMarket.Application.Comprobantes.Bridge;

// ABSTRACCIÓN del Bridge: arma QUÉ dice el comprobante y delega el CÓMO
// se entrega al ISalidaDocumento inyectado.
public abstract class ComprobanteBase
{
    private readonly ISalidaDocumento _salida;

    protected ComprobanteBase(ISalidaDocumento salida)
    {
        _salida = salida;
    }

    public string Generar() => _salida.Entregar(Titulo, ConstruirLineas());

    protected abstract string Titulo { get; }
    protected abstract IReadOnlyList<string> ConstruirLineas();
}