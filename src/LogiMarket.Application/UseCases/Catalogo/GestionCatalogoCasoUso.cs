using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Security;
using LogiMarket.Application.Validation;
using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Application.UseCases.Catalogo;

public sealed class GestionCatalogoCasoUso(
    IAdministracionCatalogoRepository administracion,
    ICatalogoRepository catalogo,
    IUnitOfWork unidadDeTrabajo,
    IUsuarioActual usuario) : IGestionCatalogo
{
    public async Task<ResultadoOperacion> GuardarProductoAsync(
        string sku, string nombre, decimal precio, decimal pesoKg, int stockInicial, CancellationToken ct = default)
    {
        if (!PuedeAdministrar()) return NoAutorizado();
        if (string.IsNullOrWhiteSpace(sku)) return ResultadoOperacion.Fallo("SKU_REQUERIDO", "El SKU es obligatorio.");
        sku = sku.Trim().ToUpperInvariant();

        try
        {
            var existente = await administracion.ObtenerProductoParaEdicionAsync(sku.Trim(), ct);
            if (existente is null)
            {
                if (await administracion.SkuExisteAsync(sku.Trim(), ct))
                    return ResultadoOperacion.Fallo("SKU_DUPLICADO", "Ya existe un producto o kit con ese SKU.");
                administracion.AgregarProducto(new Producto(sku, nombre, new Dinero(precio), pesoKg, stockInicial));
            }
            else
            {
                existente.ActualizarDatos(nombre, new Dinero(precio), pesoKg);
            }

            await unidadDeTrabajo.CommitAsync();
            return ResultadoOperacion.Ok();
        }
        catch (DomainException ex) { return ResultadoOperacion.Fallo("PRODUCTO_INVALIDO", ex.Message); }
    }

    public async Task<ResultadoOperacion> GuardarKitAsync(
        string sku, string nombre, IReadOnlyList<ComponenteKitSolicitud> componentes, CancellationToken ct = default)
    {
        if (!PuedeAdministrar()) return NoAutorizado();
        if (string.IsNullOrWhiteSpace(sku)) return ResultadoOperacion.Fallo("SKU_REQUERIDO", "El SKU es obligatorio.");
        if (componentes is null || componentes.Count == 0)
            return ResultadoOperacion.Fallo("KIT_VACIO", "El kit requiere al menos un componente.");
        sku = sku.Trim().ToUpperInvariant();

        try
        {
            var kit = new KitCompuesto(sku, nombre);
            var componentesNormalizados = componentes.Select(c => c with { Sku = c.Sku.Trim().ToUpperInvariant() }).ToArray();
            foreach (var componente in componentesNormalizados)
            {
                if (componente.Cantidad <= 0)
                    return ResultadoOperacion.Fallo("CANTIDAD_INVALIDA", "La cantidad de cada componente debe ser positiva.");
                var elemento = await catalogo.ObtenerPorSkuAsync(componente.Sku);
                if (elemento is null)
                    return ResultadoOperacion.Fallo("COMPONENTE_NO_ENCONTRADO", $"No existe el componente '{componente.Sku}'.");
                kit.Agregar(elemento, componente.Cantidad);
            }

            if (await administracion.SkuExisteAsync(sku.Trim())
                && await catalogo.ObtenerPorSkuAsync(sku.Trim()) is not KitCompuesto)
                return ResultadoOperacion.Fallo("SKU_DUPLICADO", "Ese SKU pertenece a un producto.");

            await administracion.GuardarKitAsync(kit.Sku, kit.Nombre, componentesNormalizados, ct);
            await unidadDeTrabajo.CommitAsync();
            return ResultadoOperacion.Ok();
        }
        catch (DomainException ex) { return ResultadoOperacion.Fallo("KIT_INVALIDO", ex.Message); }
    }

    public async Task<ResultadoOperacion> EliminarAsync(string sku, CancellationToken ct = default)
    {
        if (!PuedeAdministrar()) return NoAutorizado();
        sku = sku.Trim().ToUpperInvariant();
        var elemento = await catalogo.ObtenerPorSkuAsync(sku);
        if (elemento is null) return ResultadoOperacion.Fallo("NO_ENCONTRADO", "No existe el elemento seleccionado.");

        var eliminado = elemento is KitCompuesto
            ? await administracion.EliminarKitAsync(sku, ct)
            : await administracion.EliminarProductoAsync(sku, ct);
        if (!eliminado)
            return ResultadoOperacion.Fallo("ELEMENTO_EN_USO", "No se puede eliminar: el elemento aparece en kits, pedidos o movimientos.");

        await unidadDeTrabajo.CommitAsync();
        return ResultadoOperacion.Ok();
    }

    private bool PuedeAdministrar() => usuario.Rol == RolUsuario.Admin;
    private static ResultadoOperacion NoAutorizado() =>
        ResultadoOperacion.Fallo("NO_AUTORIZADO", "Solo ADMIN puede modificar el catálogo.");
}
