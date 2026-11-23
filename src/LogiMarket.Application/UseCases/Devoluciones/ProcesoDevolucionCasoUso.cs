using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Security;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Returns.TemplateMethod;

namespace LogiMarket.Application.UseCases.Devoluciones;

public sealed class ProcesoDevolucionCasoUso : IProcesoDevolucion
{
    private readonly IPedidoRepository _pedidos;
    private readonly IProductoRepository _productos;
    private readonly IDevolucionRepository _devoluciones;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly IOutbox _outbox;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IUsuarioActual _usuario;

    public ProcesoDevolucionCasoUso(
        IPedidoRepository pedidos, IProductoRepository productos, IDevolucionRepository devoluciones,
        IMovimientoInventarioRepository movimientos, IOutbox outbox, IUnitOfWork unidadDeTrabajo,
        IUsuarioActual usuario)
    {
        _pedidos = pedidos;
        _productos = productos;
        _devoluciones = devoluciones;
        _movimientos = movimientos;
        _outbox = outbox;
        _unidadDeTrabajo = unidadDeTrabajo;
        _usuario = usuario;
    }

    public async Task<ResultadoDevolucionAplicacion> SolicitarAsync(
        SolicitudDevolucionDto solicitud, CancellationToken ct = default)
    {
        if (!PoliticaDeAcceso.PuedeProcesarDevolucion(_usuario.Rol))
            return ResultadoDevolucionAplicacion.Fallo("NO_AUTORIZADO", $"El rol {_usuario.Rol} no puede procesar devoluciones.");

        var pedido = await _pedidos.ObtenerPorIdAsync(solicitud.PedidoId);
        if (pedido is null)
            return ResultadoDevolucionAplicacion.Fallo("PEDIDO_NO_ENCONTRADO", "El pedido no existe.");

        // Se elige la VARIANTE del Template Method (Paso 8) según el motivo. Ninguna regla
        // de plazo o de condición se repite aquí: ya vive en Domain.
        ProcesarDevolucion proceso = solicitud.Motivo switch
        {
            MotivoDevolucion.Defecto => new DevolucionPorDefecto(),
            MotivoDevolucion.Arrepentimiento => new DevolucionPorArrepentimiento(),
            _ => throw new ArgumentOutOfRangeException(nameof(solicitud), "Motivo de devolución no reconocido.")
        };

        var solicitudDominio = new SolicitudDevolucion(pedido, solicitud.FechaSolicitudUtc, solicitud.ProductoSinUso);
        var resultado = proceso.Ejecutar(solicitudDominio);

        if (!resultado.Aceptada)
            return ResultadoDevolucionAplicacion.Fallo("DEVOLUCION_RECHAZADA", resultado.MotivoRechazo!);

        var devolucion = resultado.Devolucion!;

        // El dominio ya dijo QUÉ reintegrar (Devolucion.Reintegros); aquí solo se aplica.
        var productos = (await _productos.ObtenerPorSkusAsync(devolucion.Reintegros.Select(r => r.Sku)))
            .ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);

        foreach (var reintegro in devolucion.Reintegros)
        {
            productos[reintegro.Sku].ReintegrarStock(reintegro.Cantidad);
            _movimientos.Agregar(new MovimientoInventario(
                reintegro.Sku, reintegro.Cantidad, MotivoMovimiento.Devolucion, pedido.Id, devolucion.FechaUtc));
        }

        _devoluciones.Agregar(devolucion);
        _outbox.Registrar(resultado.Evento!);

        try
        {
            await _unidadDeTrabajo.CommitAsync();
        }
        catch (ConflictoConcurrenciaException)
        {
            return ResultadoDevolucionAplicacion.Fallo("CONFLICTO_CONCURRENCIA", "Otro proceso modificó los datos. Intente de nuevo.");
        }

        return ResultadoDevolucionAplicacion.Ok(devolucion);
    }
}