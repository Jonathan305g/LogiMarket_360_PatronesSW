using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Security;

// Una sola fuente de verdad sobre quién puede qué.
public static class PoliticaDeAcceso
{
    public static bool PuedeCobrar(RolUsuario rol) =>
        rol is RolUsuario.Admin or RolUsuario.Ventas;
    public static bool PuedeCancelar(RolUsuario rol) =>
    rol is RolUsuario.Admin or RolUsuario.Ventas;
}