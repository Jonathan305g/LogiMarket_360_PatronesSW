using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Tests.Fakes;

public sealed class AuditoriaSeguridadDePrueba : IAuditoriaSeguridad
{
    public List<(string Usuario, string Rol, string Operacion)> Registros { get; } = new();

    public Task RegistrarAsync(string usuario, string rol, string operacion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Registros.Add((usuario, rol, operacion));
        return Task.CompletedTask;
    }

    public void RegistrarIntentoNoAutorizado(string usuario, RolUsuario rol, string operacion) =>
        Registros.Add((usuario, rol.ToString(), operacion));
}
