using LogiMarket.Application.Abstractions.Services;

namespace LogiMarket.Infrastructure.Payments.Legacy;

// Proveedor LEGADO simulado: API incompatible con el negocio (centavos, síncrona,
// nombre de operación propio). Representa un SDK de terceros que no se puede tocar.
public class PasarelaLegadaSimulada : IPasarelaPagoDemo
{
    private readonly HashSet<string> _clavesUsadas = new();
    private int _rechazarSiguiente;

    public void RechazarSiguienteCobro() => Interlocked.Exchange(ref _rechazarSiguiente, 1);

    public (bool Ok, string Codigo) CobrarCentavos(long centavos, string clave)
    {
        if (Interlocked.Exchange(ref _rechazarSiguiente, 0) == 1)
            return (false, "RECHAZO_SIMULADO");

        if (!_clavesUsadas.Add(clave))
            return (false, "CLAVE_DUPLICADA");

        if (centavos <= 0)
            return (false, "MONTO_INVALIDO");

        return (true, ("LEG-" + Guid.NewGuid().ToString("N"))[..12].ToUpperInvariant());
    }
}
