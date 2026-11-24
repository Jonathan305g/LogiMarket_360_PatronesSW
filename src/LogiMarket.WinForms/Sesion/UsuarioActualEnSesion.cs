using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.WinForms.Sesion;

public sealed class UsuarioActualEnSesion : IUsuarioActual
{
    public Guid? UsuarioId { get; private set; }
    public string? NombreUsuario { get; private set; }
    public string Nombre => NombreUsuario ?? string.Empty;
    public RolUsuario Rol { get; private set; } = RolUsuario.Consulta;
    public bool EstaAutenticado => UsuarioId is not null;

    public void IniciarSesion(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        UsuarioId = usuario.Id;
        NombreUsuario = usuario.NombreUsuario;
        Rol = usuario.Rol;
    }

    public void CerrarSesion()
    {
        UsuarioId = null;
        NombreUsuario = null;
        Rol = RolUsuario.Consulta;
    }
}
