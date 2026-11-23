using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.Security;

// Una sola fuente de verdad sobre quién puede qué 
public static class PoliticaDeAcceso
{
    public static bool PuedeCobrar(RolUsuario rol) =>
        rol is RolUsuario.Admin or RolUsuario.Ventas;

    public static bool PuedeCancelar(RolUsuario rol) =>
        rol is RolUsuario.Admin or RolUsuario.Ventas;

    // despachar y entregar son tareas de bodega.
    public static bool PuedeDespachar(RolUsuario rol) =>
        rol is RolUsuario.Admin or RolUsuario.Bodega;

    public static bool PuedeEntregar(RolUsuario rol) =>
        rol is RolUsuario.Admin or RolUsuario.Bodega;

    // la devolución la gestiona quien atiende al cliente.
    public static bool PuedeProcesarDevolucion(RolUsuario rol) =>
        rol is RolUsuario.Admin or RolUsuario.Ventas;
}