using LogiMarket.Application.Abstractions.Gateways;

namespace LogiMarket.Infrastructure.Payments.Adapter;

// Segundo proveedor, con API ya compatible de origen (no necesita adaptador).
// Demuestra que IPasarelaPago admite implementaciones intercambiables de verdad.
public class PasarelaModernaSimulada : IPasarelaPago
{
    private readonly HashSet<string> _clavesUsadas = new();

    public Task<ResultadoCobro> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
    {
        if (!_clavesUsadas.Add(claveIdempotencia))
            return Task.FromResult(new ResultadoCobro(false, string.Empty, "CLAVE_DUPLICADA"));

        if (monto <= 0)
            return Task.FromResult(new ResultadoCobro(false, string.Empty, "MONTO_INVALIDO"));

        return Task.FromResult(new ResultadoCobro(true, ("MOD-" + Guid.NewGuid().ToString("N"))[..12].ToUpperInvariant(), null));
    }
}