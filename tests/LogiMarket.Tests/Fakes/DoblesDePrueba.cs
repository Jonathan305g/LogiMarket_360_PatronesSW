using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Tests.Fakes;

// Pasarela de prueba que ANOTA cada llamada que recibe.
public sealed class PasarelaEspia : IPasarelaPago
{
    public List<(decimal Monto, string Clave)> Llamadas { get; } = new();
    public int Invocaciones => Llamadas.Count;

    public Task<ResultadoCobro> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
    {
        Llamadas.Add((monto, claveIdempotencia));
        return Task.FromResult(ResultadoCobro.Exito("REF-ESPIA"));
    }
}

public sealed class UsuarioActualFalso : IUsuarioActual
{
    public UsuarioActualFalso(RolUsuario rol, string nombre = "tester")
    {
        Rol = rol;
        Nombre = nombre;
    }

    public string Nombre { get; }
    public RolUsuario Rol { get; }
}

// Auditoría de seguridad de prueba que ANOTA los intentos denegados.
public sealed class AuditoriaSeguridadEspia : IAuditoriaSeguridad
{
    public List<(string Usuario, RolUsuario Rol, string Operacion)> Intentos { get; } = new();

    public void RegistrarIntentoNoAutorizado(string usuario, RolUsuario rol, string operacion) =>
        Intentos.Add((usuario, rol, operacion));
}