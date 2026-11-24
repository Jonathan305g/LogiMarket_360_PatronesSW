using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Entities;

// Cuenta de acceso local. La contraseña se guarda como HASH; esta clase nunca ve texto claro.
public class Usuario
{
    public Guid Id { get; private set; }
    public string NombreUsuario { get; private set; }
    public string HashContrasena { get; private set; }
    public RolUsuario Rol { get; private set; }
    public bool Activo { get; private set; }

    public Usuario(string nombreUsuario, string hashContrasena, RolUsuario rol)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new DomainException("El usuario requiere un nombre.");
        if (string.IsNullOrWhiteSpace(hashContrasena))
            throw new DomainException("El usuario requiere un hash de contraseña.");

        Id = Guid.NewGuid();
        NombreUsuario = nombreUsuario.Trim();
        HashContrasena = hashContrasena;
        Rol = rol;
        Activo = true;
    }

    private Usuario() { NombreUsuario = string.Empty; HashContrasena = string.Empty; } // EF Core

    public void ActualizarHashContrasena(string hashContrasena)
    {
        if (string.IsNullOrWhiteSpace(hashContrasena))
            throw new DomainException("El hash de contraseña es obligatorio.");

        HashContrasena = hashContrasena;
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
