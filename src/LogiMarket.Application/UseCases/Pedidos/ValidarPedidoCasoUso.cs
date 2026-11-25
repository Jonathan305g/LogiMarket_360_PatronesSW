using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Security;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Application.UseCases.Pedidos;

public sealed record ResultadoValidacionPedido(bool Exitoso, Pedido? Pedido, CotizacionPedido? Cotizacion, string? Error);

public interface IValidarPedidoCasoUso
{
    Task<ResultadoValidacionPedido> EjecutarAsync(Pedido borrador, CategoriaCliente categoria);
}

public sealed class ValidarPedidoCasoUso(
    IPedidoRepository pedidos,
    IUnitOfWork unidadDeTrabajo,
    ICotizadorPedido cotizador,
    IUsuarioActual usuario) : IValidarPedidoCasoUso
{
    public async Task<ResultadoValidacionPedido> EjecutarAsync(Pedido borrador, CategoriaCliente categoria)
    {
        ArgumentNullException.ThrowIfNull(borrador);

        if (!PoliticaDeAcceso.PuedeValidarPedido(usuario.Rol))
            return new(false, null, null, $"El rol {usuario.Rol} no puede validar pedidos.");

        if (borrador.Detalles.Count == 0)
            return new(false, null, null, "El pedido requiere al menos una línea.");

        try
        {
            var cotizacion = cotizador.Cotizar(borrador, categoria);
            var pedidoValidado = borrador.Clonar();
            pedidoValidado.Validar();
            pedidoValidado.Valorar(cotizacion.Descuento, cotizacion.Tarifa);

            pedidos.Agregar(pedidoValidado);
            await unidadDeTrabajo.CommitAsync();
            return new(true, pedidoValidado, cotizacion, null);
        }
        catch (DomainException ex)
        {
            return new(false, null, null, ex.Message);
        }
    }
}
