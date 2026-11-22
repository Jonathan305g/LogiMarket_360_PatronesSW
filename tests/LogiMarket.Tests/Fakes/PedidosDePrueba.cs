using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Tests.Fakes;

public static class PedidosDePrueba
{
    // Pedido de Ana (VIP): KIT_TELETRABAJO x1, ESTÁNDAR. Subtotal 60.00.
    public static Pedido Borrador() =>
        new PedidoBuilder()
            .ConCliente(new Cliente("Ana", CategoriaCliente.Vip))
            .ConEntrega(TipoEntrega.Estandar)
            .AgregarLinea(CatalogoDePrueba.CrearKitTeletrabajo(), 1)
            .Construir();

    // El mismo pedido de Ana ya ENTREGADO: subtotal 60, descuento 6, tarifa 5, total 59.
    public static Pedido EntregadoDeAna()
    {
        var pedido = Borrador();
        pedido.Validar();
        pedido.Valorar(new Dinero(6m), new Dinero(5m));
        pedido.Pagar();
        pedido.Despachar();
        pedido.Entregar();
        return pedido;
    }
}