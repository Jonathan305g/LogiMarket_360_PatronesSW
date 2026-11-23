using LogiMarket.Application.Commands.Command;
using LogiMarket.Application.UseCases.ConfirmarPedido.Chain;
using LogiMarket.Application.UseCases.ConfirmarPedido.Facade;
using LogiMarket.Application.UseCases.Pedidos;
using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Delivery.AbstractFactory;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.Pricing.Strategy;

namespace LogiMarket.Tests.Fakes;

// Todo el "mundo" de una prueba: almacén falso, pasarela espía, usuario y los servicios ya armados.
public sealed class Escenario
{
    public AlmacenEnMemoria Almacen { get; } = AlmacenEnMemoria.ConCatalogoOficial();
    public PasarelaEspia Pasarela { get; } = new();
    public UsuarioActualFalso Usuario { get; }
    public ConfirmacionPedidoFacade Facade { get; }
    public CancelarPedidoCasoUso Cancelacion { get; }
    public InvocadorComandos Invocador { get; }

    public Escenario(RolUsuario rol = RolUsuario.Ventas)
    {
        Usuario = new UsuarioActualFalso(rol);

        Facade = new ConfirmacionPedidoFacade(
            CadenaValidacionConfirmacion.Crear(),
            Almacen,   // IClienteRepository
            Almacen,   // IPedidoRepository
            Almacen,   // IProductoRepository
            Almacen,   // IPagoRepository
            Almacen,   // IMovimientoInventarioRepository
            Almacen,   // IOutbox
            Almacen,   // IUnitOfWork
            new CalculadoraDescuento(new IPoliticaDescuento[] { new SinDescuento(), new Vip10(), new Mayorista15() }),
            new PlanificadorEntrega(new IFabricaEntrega[] { new FabricaEntregaEstandar(), new FabricaEntregaExpres() }),
            Pasarela,
            Usuario);

        Cancelacion = new CancelarPedidoCasoUso(Almacen, Almacen, Usuario);
        Invocador = new InvocadorComandos(Usuario);
    }

    public Task<ResultadoConfirmacion> Confirmar(Pedido pedido, string clave) =>
        Facade.ConfirmarAsync(new ConfirmarPedidoSolicitud(pedido.Id, clave));

    public int Stock(string sku) => Almacen.ProductoPorSku(sku).Stock;

    public static Pedido PedidoDe(Cliente cliente, TipoEntrega entrega, params (IElementoCatalogo Item, int Cantidad)[] lineas)
    {
        var builder = new PedidoBuilder().ConCliente(cliente).ConEntrega(entrega);
        foreach (var (item, cantidad) in lineas)
            builder.AgregarLinea(item, cantidad);
        return builder.Construir();
    }
}