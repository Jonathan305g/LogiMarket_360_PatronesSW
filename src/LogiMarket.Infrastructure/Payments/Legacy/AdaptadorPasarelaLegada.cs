using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Infrastructure.Payments.Legacy;

namespace LogiMarket.Infrastructure.Payments.Adapter;

// ADAPTER: traduce la interfaz legada (centavos, síncrona) a IPasarelaPago
// (decimales, asíncrona, con CancellationToken), que es lo que el negocio espera.
public class AdaptadorPasarelaLegada : IPasarelaPago
{
    private readonly PasarelaLegadaSimulada _legada;

    public AdaptadorPasarelaLegada(PasarelaLegadaSimulada legada)
    {
        _legada = legada;
    }

    public Task<ResultadoCobro> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
    {
        // La traducción vive AQUÍ, en un solo lugar: decimal -> centavos (long).
        long centavos = (long)Math.Round(monto * 100m, MidpointRounding.AwayFromZero);
        var (ok, codigo) = _legada.CobrarCentavos(centavos, claveIdempotencia);

        var resultado = ok
            ? ResultadoCobro.Exito(codigo)
            : ResultadoCobro.Rechazo(codigo);

        return Task.FromResult(resultado);
    }
}
