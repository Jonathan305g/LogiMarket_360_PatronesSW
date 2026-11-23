using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Security;
using LogiMarket.Application.Validation;
using LogiMarket.Domain.Delivery.AbstractFactory;
using LogiMarket.Domain.Events;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Application.UseCases.Despacho;

public sealed class GestionDespachoCasoUso : IGestionDespacho
{
    private readonly IPedidoRepository _pedidos;
    private readonly IDespachoRepository _despachos;
    private readonly IOutbox _outbox;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly PlanificadorEntrega _planificador;   // Abstract Factory: la MISMA pieza del Paso 8
    private readonly IUsuarioActual _usuario;

    public GestionDespachoCasoUso(
        IPedidoRepository pedidos, IDespachoRepository despachos, IOutbox outbox,
        IUnitOfWork unidadDeTrabajo, PlanificadorEntrega planificador, IUsuarioActual usuario)
    {
        _pedidos = pedidos;
        _despachos = despachos;
        _outbox = outbox;
        _unidadDeTrabajo = unidadDeTrabajo;
        _planificador = planificador;
        _usuario = usuario;
    }

    public async Task<ResultadoDespacho> DespacharAsync(Guid pedidoId, CancellationToken ct = default)
    {
        if (!PoliticaDeAcceso.PuedeDespachar(_usuario.Rol))
            return ResultadoDespacho.Fallo("NO_AUTORIZADO", $"El rol {_usuario.Rol} no puede despachar pedidos.");

        var pedido = await _pedidos.ObtenerPorIdAsync(pedidoId);
        if (pedido is null)
            return ResultadoDespacho.Fallo("PEDIDO_NO_ENCONTRADO", "El pedido no existe.");

        // La regla "no despachar antes de pagar" la decide el State del dominio, no esta clase.
        try
        {
            pedido.Despachar();
        }
        catch (DomainException ex)
        {
            return ResultadoDespacho.Fallo("TRANSICION_INVALIDA", ex.Message);
        }

        var ahora = DateTime.UtcNow;

        // Se pide la etiqueta/guía a la MISMA fábrica de la familia del pedido (ESTÁNDAR/EXPRÉS).
        var plan = _planificador.Planificar(pedido, ahora);

        _despachos.Agregar(new Domain.Entities.Despacho(pedido.Id, plan.Etiqueta, ahora));
        _outbox.Registrar(new DespachoRegistrado(pedido.Id, plan.Etiqueta, ahora));

        try
        {
            await _unidadDeTrabajo.CommitAsync();
        }
        catch (ConflictoConcurrenciaException)
        {
            return ResultadoDespacho.Fallo("CONFLICTO_CONCURRENCIA", "Otro proceso modificó el pedido. Intente de nuevo.");
        }

        return ResultadoDespacho.Ok(plan.Etiqueta);
    }

    public async Task<ResultadoOperacion> EntregarAsync(Guid pedidoId, CancellationToken ct = default)
    {
        if (!PoliticaDeAcceso.PuedeEntregar(_usuario.Rol))
            return ResultadoOperacion.Fallo("NO_AUTORIZADO", $"El rol {_usuario.Rol} no puede entregar pedidos.");

        var pedido = await _pedidos.ObtenerPorIdAsync(pedidoId);
        if (pedido is null)
            return ResultadoOperacion.Fallo("PEDIDO_NO_ENCONTRADO", "El pedido no existe.");

        try
        {
            pedido.Entregar(); // guarda FechaEntregaUtc (Paso 8)
        }
        catch (DomainException ex)
        {
            return ResultadoOperacion.Fallo("TRANSICION_INVALIDA", ex.Message);
        }

        try
        {
            await _unidadDeTrabajo.CommitAsync();
        }
        catch (ConflictoConcurrenciaException)
        {
            return ResultadoOperacion.Fallo("CONFLICTO_CONCURRENCIA", "Otro proceso modificó el pedido. Intente de nuevo.");
        }

        return ResultadoOperacion.Ok();
    }
}