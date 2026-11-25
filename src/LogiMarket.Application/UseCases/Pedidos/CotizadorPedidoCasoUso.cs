using LogiMarket.Domain.Delivery.AbstractFactory;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Pricing.Strategy;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Application.UseCases.Pedidos;

public sealed record CotizacionPedido(Dinero Subtotal, Dinero Descuento, Dinero Tarifa, Dinero Total, string Politica);

public interface ICotizadorPedido
{
    CotizacionPedido Cotizar(Pedido pedido, CategoriaCliente categoria);
}

public sealed class CotizadorPedidoCasoUso(
    CalculadoraDescuento calculadoraDescuento,
    PlanificadorEntrega planificadorEntrega) : ICotizadorPedido
{
    public CotizacionPedido Cotizar(Pedido pedido, CategoriaCliente categoria)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var subtotal = pedido.Subtotal;
        var descuento = calculadoraDescuento.Calcular(
            subtotal,
            new ContextoDescuento(categoria, pedido.UnidadesFisicas));
        var tarifa = planificadorEntrega.Planificar(pedido, DateTime.UtcNow).Tarifa;
        var total = subtotal - descuento.Monto + tarifa;

        return new CotizacionPedido(subtotal, descuento.Monto, tarifa, total, descuento.Politica);
    }
}
