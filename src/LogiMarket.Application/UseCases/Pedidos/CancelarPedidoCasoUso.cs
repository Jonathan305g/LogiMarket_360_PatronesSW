using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Security;
using LogiMarket.Application.Validation;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Application.UseCases.Pedidos;

public sealed class CancelarPedidoCasoUso : ICancelacionPedido
{
    private readonly IPedidoRepository _pedidos;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IUsuarioActual _usuario;

    public CancelarPedidoCasoUso(IPedidoRepository pedidos, IUnitOfWork unidadDeTrabajo, IUsuarioActual usuario)
    {
        _pedidos = pedidos;
        _unidadDeTrabajo = unidadDeTrabajo;
        _usuario = usuario;
    }

    public async Task<ResultadoOperacion> CancelarAsync(Guid pedidoId, CancellationToken ct = default)
    {
        // 1) Permiso: se comprueba AQUÍ, no en el botón.
        if (!PoliticaDeAcceso.PuedeCancelar(_usuario.Rol))
            return ResultadoOperacion.Fallo("NO_AUTORIZADO", $"El rol {_usuario.Rol} no puede cancelar pedidos.");

        var pedido = await _pedidos.ObtenerPorIdAsync(pedidoId);
        if (pedido is null)
            return ResultadoOperacion.Fallo("PEDIDO_NO_ENCONTRADO", "El pedido no existe.");

        // 2) La regla de qué estados se pueden cancelar la decide el State del dominio.
        try
        {
            pedido.Cancelar();
        }
        catch (DomainException ex)
        {
            return ResultadoOperacion.Fallo("TRANSICION_INVALIDA", ex.Message);
        }

        // 3) Se guarda.
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