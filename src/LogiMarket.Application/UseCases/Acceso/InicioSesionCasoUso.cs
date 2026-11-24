using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.UseCases.Acceso;

public sealed record ResultadoInicioSesion(bool Exitoso, Usuario? Usuario, string? Error);

public interface IInicioSesionCasoUso
{
    Task<ResultadoInicioSesion> IniciarSesionAsync(string nombreUsuario, string contrasena, CancellationToken ct = default);
}

public sealed class InicioSesionCasoUso : IInicioSesionCasoUso
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IHasherContrasena _hasher;
    private readonly IAuditoriaSeguridad _auditoria;

    public InicioSesionCasoUso(IUsuarioRepository usuarios, IHasherContrasena hasher, IAuditoriaSeguridad auditoria)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _auditoria = auditoria;
    }

    public async Task<ResultadoInicioSesion> IniciarSesionAsync(string nombreUsuario, string contrasena, CancellationToken ct = default)
    {
        var nombre = nombreUsuario?.Trim() ?? string.Empty;
        var usuario = string.IsNullOrEmpty(nombre)
            ? null
            : await _usuarios.ObtenerPorNombreUsuarioAsync(nombre, ct);

        if (usuario is null || !usuario.Activo || !_hasher.Verificar(contrasena, usuario.HashContrasena))
        {
            await _auditoria.RegistrarAsync(nombre, "Desconocido", "IniciarSesion: credenciales inválidas", ct);
            return new ResultadoInicioSesion(false, null, "Usuario o contraseña incorrectos.");
        }

        return new ResultadoInicioSesion(true, usuario, null);
    }
}
