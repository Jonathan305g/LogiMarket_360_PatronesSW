using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.UseCases.ConfirmarPedido.Chain;

// Todo lo que los eslabones necesitan saber, ya cargado. La cadena no consulta nada por su cuenta.
public sealed record ContextoConfirmacion(
    Pedido Pedido,
    Cliente? Cliente,                              // null = no se encontró
    IReadOnlyDictionary<string, int> StockDisponible, // SKU físico -> unidades en inventario
    RolUsuario Rol);