using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Orders.Builder;

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
}