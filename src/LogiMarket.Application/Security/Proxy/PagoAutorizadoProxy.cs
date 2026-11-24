using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Abstractions.Services;

namespace LogiMarket.Application.Security.Proxy;

// Implementa la MISMA interfaz que la pasarela real, así quien cobra no nota la diferencia.
public sealed class PagoAutorizadoProxy : IPasarelaPago
{
    private readonly IPasarelaPago _siguiente;      // lo que hay detrás (después: Decorator -> Adapter)
    private readonly IUsuarioActual _usuario;
    private readonly IAuditoriaSeguridad _auditoria;

    public PagoAutorizadoProxy(IPasarelaPago siguiente, IUsuarioActual usuario, IAuditoriaSeguridad auditoria)
    {
        _siguiente = siguiente ?? throw new ArgumentNullException(nameof(siguiente));
        _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<ResultadoCobro> CobrarAsync(
        decimal monto, string claveIdempotencia, CancellationToken ct = default)
    {
        // 1) Control de acceso ANTES de tocar nada más.
        if (!PoliticaDeAcceso.PuedeCobrar(_usuario.Rol))
        {
            // El intento queda en la auditoría de SEGURIDAD, no en el log del cobro.
            await _auditoria.RegistrarAsync(_usuario.Nombre, _usuario.Rol.ToString(), "COBRAR", ct);
            throw new AccesoDenegadoException($"El rol {_usuario.Rol} no puede realizar cobros.");
        }

        // 2) Autorizado: se delega tal cual en la siguiente capa.
        return await _siguiente.CobrarAsync(monto, claveIdempotencia, ct);
    }
}
