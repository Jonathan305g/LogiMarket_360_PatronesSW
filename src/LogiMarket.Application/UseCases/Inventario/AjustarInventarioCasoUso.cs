using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Security;
using LogiMarket.Application.Validation;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Application.UseCases.Inventario;

public sealed record SolicitudAjusteInventario(string Sku, int Cantidad);

public interface IAjustarInventarioCasoUso
{
    Task<ResultadoOperacion> EjecutarAsync(SolicitudAjusteInventario solicitud, CancellationToken ct = default);
}

public sealed class AjustarInventarioCasoUso(
    IProductoRepository productos,
    IMovimientoInventarioRepository movimientos,
    IUnitOfWork unidadDeTrabajo,
    IUsuarioActual usuario) : IAjustarInventarioCasoUso
{
    public async Task<ResultadoOperacion> EjecutarAsync(SolicitudAjusteInventario solicitud, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        if (!PoliticaDeAcceso.PuedeAjustarInventario(usuario.Rol))
            return ResultadoOperacion.Fallo("NO_AUTORIZADO", $"El rol {usuario.Rol} no puede ajustar inventario.");
        if (string.IsNullOrWhiteSpace(solicitud.Sku))
            return ResultadoOperacion.Fallo("SKU_REQUERIDO", "Selecciona un SKU.");
        if (solicitud.Cantidad == 0)
            return ResultadoOperacion.Fallo("CANTIDAD_INVALIDA", "La cantidad a ajustar no puede ser cero.");

        var producto = (await productos.ObtenerPorSkusAsync(new[] { solicitud.Sku }))
            .FirstOrDefault();
        if (producto is null)
            return ResultadoOperacion.Fallo("PRODUCTO_NO_ENCONTRADO", $"No se encontró el producto con SKU {solicitud.Sku}.");

        try
        {
            if (solicitud.Cantidad > 0)
                producto.ReintegrarStock(solicitud.Cantidad);
            else
                producto.DescontarStock(-solicitud.Cantidad);

            movimientos.Agregar(new MovimientoInventario(
                producto.Sku, solicitud.Cantidad, MotivoMovimiento.Ajuste, Guid.NewGuid(), DateTime.UtcNow));
            await unidadDeTrabajo.CommitAsync();
            return ResultadoOperacion.Ok();
        }
        catch (DomainException ex)
        {
            return ResultadoOperacion.Fallo("AJUSTE_RECHAZADO", ex.Message);
        }
        catch (ConflictoConcurrenciaException)
        {
            return ResultadoOperacion.Fallo("CONFLICTO_CONCURRENCIA", "El inventario cambió en otra operación. Actualiza y vuelve a intentar.");
        }
    }
}
