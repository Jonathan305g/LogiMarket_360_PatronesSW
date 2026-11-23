using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.UseCases.ConfirmarPedido.Chain;
using LogiMarket.Application.Validation;
using LogiMarket.Domain.Delivery.AbstractFactory;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Events;
using LogiMarket.Domain.Pricing.Strategy;

namespace LogiMarket.Application.UseCases.ConfirmarPedido.Facade;

// FACADE: una sola operación para el exterior; por dentro coordina a los especialistas.
// No contiene SQL ni reglas de descuento: cada cosa la hace su colaborador.
public sealed class ConfirmacionPedidoFacade : IConfirmacionPedido
{
    private readonly EslabonValidacion _cadena;
    private readonly IClienteRepository _clientes;
    private readonly IPedidoRepository _pedidos;
    private readonly IProductoRepository _productos;
    private readonly IPagoRepository _pagos;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly IOutbox _outbox;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly CalculadoraDescuento _calculadora;
    private readonly PlanificadorEntrega _planificador;
    private readonly IPasarelaPago _pasarela;
    private readonly IUsuarioActual _usuario;

    public ConfirmacionPedidoFacade(
        EslabonValidacion cadena,
        IClienteRepository clientes,
        IPedidoRepository pedidos,
        IProductoRepository productos,
        IPagoRepository pagos,
        IMovimientoInventarioRepository movimientos,
        IOutbox outbox,
        IUnitOfWork unidadDeTrabajo,
        CalculadoraDescuento calculadora,
        PlanificadorEntrega planificador,
        IPasarelaPago pasarela,
        IUsuarioActual usuario)
    {
        _cadena = cadena;
        _clientes = clientes;
        _pedidos = pedidos;
        _productos = productos;
        _pagos = pagos;
        _movimientos = movimientos;
        _outbox = outbox;
        _unidadDeTrabajo = unidadDeTrabajo;
        _calculadora = calculadora;
        _planificador = planificador;
        _pasarela = pasarela;
        _usuario = usuario;
    }

    public async Task<ResultadoConfirmacion> ConfirmarAsync(
        ConfirmarPedidoSolicitud solicitud, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var clave = solicitud.ClaveIdempotencia;
        if (string.IsNullOrWhiteSpace(clave))
            return Rechazo("CLAVE_REQUERIDA", "La confirmación requiere una clave de idempotencia.");

        // 1) IDEMPOTENCIA: si esta clave ya cobró, se devuelve lo que se hizo la primera vez.
        var previo = await _pagos.ObtenerPorClaveAsync(clave);
        if (previo is not null)
        {
            return previo.PedidoId == solicitud.PedidoId
                ? ResultadoConfirmacion.Repetido(previo)
                : Rechazo("CLAVE_REUTILIZADA", "Esa clave de idempotencia ya pertenece a otro pedido.");
        }

        // 2) CARGAR lo necesario.
        var pedido = await _pedidos.ObtenerPorIdAsync(solicitud.PedidoId);
        if (pedido is null)
            return Rechazo("PEDIDO_NO_ENCONTRADO", "El pedido no existe.");

        var cliente = await _clientes.ObtenerPorIdAsync(pedido.ClienteId);
        var requeridos = pedido.ComponentesFisicosTotales();
        var productos = (await _productos.ObtenerPorSkusAsync(requeridos.Select(c => c.Sku)))
            .ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);
        var stock = productos.ToDictionary(p => p.Key, p => p.Value.Stock, StringComparer.OrdinalIgnoreCase);

        // 3) VALIDAR con la cadena. El primer problema detiene todo, sin efectos.
        var validacion = _cadena.Validar(new ContextoConfirmacion(pedido, cliente, stock, _usuario.Rol));
        if (!validacion.Aprobada)
            return ResultadoConfirmacion.Rechazado(validacion.Error!);

        // 4) VALORAR: calcular importes SIN tocar el pedido (todavía no se cobró).
        var ahora = DateTime.UtcNow;
        var descuento = _calculadora.Calcular(
            pedido.Subtotal, new ContextoDescuento(cliente!.Categoria, pedido.UnidadesFisicas));
        var tarifa = _planificador.Planificar(pedido, ahora).Tarifa;
        var total = pedido.Subtotal - descuento.Monto + tarifa;

        // 5) COBRAR. Si lo rechazan, se sale sin haber cambiado nada.
        var cobro = await _pasarela.CobrarAsync(total.Valor, clave, ct);
        if (!cobro.Aprobado)
            return Rechazo("COBRO_RECHAZADO", cobro.MotivoRechazo ?? "La pasarela rechazó el cobro.");

        // 6) APLICAR los cambios (todavía solo en memoria / pendientes).
        if (pedido.Estado == EstadoPedido.Borrador)
            pedido.Validar();
        pedido.Valorar(descuento.Monto, tarifa);
        pedido.Pagar();

        foreach (var componente in requeridos)
        {
            productos[componente.Sku].DescontarStock(componente.Cantidad);
            _movimientos.Agregar(new MovimientoInventario(
                componente.Sku, -componente.Cantidad, MotivoMovimiento.Venta, pedido.Id, ahora));
        }

        var pago = new Pago(pedido.Id, total, cobro.Referencia!, clave, ahora);
        _pagos.Agregar(pago);
        _outbox.Registrar(new PedidoPagado(pedido.Id, pedido.ClienteId, total, cobro.Referencia!, ahora));

        // 7) UNA SOLA TRANSACCIÓN: pedido, pago, stock, movimientos y Outbox, o nada.
        try
        {
            await _unidadDeTrabajo.CommitAsync();
        }
        catch (ConflictoConcurrenciaException)
        {
            return Rechazo("CONFLICTO_CONCURRENCIA",
                "Otro proceso modificó los mismos datos. No se guardó nada; intente de nuevo.");
        }

        return ResultadoConfirmacion.Confirmado(pago);
    }

    private static ResultadoConfirmacion Rechazo(string codigo, string mensaje) =>
        ResultadoConfirmacion.Rechazado(new ErrorValidacion(codigo, mensaje));
}