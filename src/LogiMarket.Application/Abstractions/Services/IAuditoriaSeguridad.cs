using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Abstractions.Services;

// Registro de intentos NO autorizados. Es independiente del log de cobros.
public interface IAuditoriaSeguridad
{
    void RegistrarIntentoNoAutorizado(string usuario, RolUsuario rol, string operacion);
}