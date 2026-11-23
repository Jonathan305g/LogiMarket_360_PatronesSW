using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Abstractions.Services;

// Quién está usando el sistema ahora mismo. WinForms/Infrastructure lo implementarán con la sesión.
public interface IUsuarioActual
{
    string Nombre { get; }
    RolUsuario Rol { get; }
}