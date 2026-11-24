using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Abstractions.Services;

// Registro de intentos NO autorizados. Es independiente del log de cobros.
public interface IAuditoriaSeguridad
{
    void RegistrarIntentoNoAutorizado(string usuario, RolUsuario rol, string operacion);

    Task RegistrarAsync(string usuario, string rol, string operacion, CancellationToken ct = default)
    {
        RegistrarIntentoNoAutorizado(usuario, Enum.Parse<RolUsuario>(rol, ignoreCase: true), operacion);
        return Task.CompletedTask;
    }

    Task<IReadOnlyList<RegistroAuditoriaSeguridadDto>> ListarRecientesAsync(int limite = 100, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RegistroAuditoriaSeguridadDto>>(Array.Empty<RegistroAuditoriaSeguridadDto>());
}
