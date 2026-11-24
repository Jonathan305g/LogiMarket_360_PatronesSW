using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Abstractions.Services;

namespace LogiMarket.Infrastructure.Payments.Decorator;

// DECORATOR: añade auditoría (INICIO/FIN/ERROR con id de correlación) alrededor
// de CUALQUIER IPasarelaPago, sin saber ni importarle qué hay por dentro.
public class AuditoriaPagoDecorator : IPasarelaPago
{
    private readonly IPasarelaPago _interno;
    private readonly IBitacoraAuditoria _bitacora;

    public AuditoriaPagoDecorator(IPasarelaPago interno, IBitacoraAuditoria bitacora)
    {
        _interno = interno;
        _bitacora = bitacora;
    }

    public async Task<ResultadoCobro> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
    {
        var correlacion = Guid.NewGuid();
        await _bitacora.RegistrarAsync("CobroPasarela", $"INICIO correlacion={correlacion} monto={monto:F2} clave={claveIdempotencia}", ct);

        try
        {
            var resultado = await _interno.CobrarAsync(monto, claveIdempotencia, ct);

            await _bitacora.RegistrarAsync("CobroPasarela",
                resultado.Aprobado
                    ? $"FIN correlacion={correlacion} referencia={resultado.Referencia}"
                    : $"FIN correlacion={correlacion} rechazado={resultado.MotivoRechazo}",
                ct);

            return resultado;
        }
        catch (Exception ex)
        {
            await _bitacora.RegistrarAsync("CobroPasarela", $"ERROR correlacion={correlacion} excepcion={ex.Message}", ct);
            throw;
        }
    }
}
