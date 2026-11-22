using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Events;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Returns.TemplateMethod;

public abstract class ProcesarDevolucion
{
    // ---- Lo que cada variante DEBE definir ----
    protected abstract MotivoDevolucion Motivo { get; }
    protected abstract int PlazoMaximoDias { get; }

    // Devuelve null si el producto está en condición aceptable, o el motivo del rechazo.
    protected abstract string? InspeccionarCondicion(SolicitudDevolucion solicitud);

    // ---- TEMPLATE METHOD ----
    // Público y NO virtual: nadie puede alterar el orden de los pasos.
    public ResultadoDevolucion Ejecutar(SolicitudDevolucion solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        // Pasos 1 y 2. El primer rechazo corta el proceso: no se calcula ni se registra nada.
        var rechazo = VerificarElegibilidad(solicitud) ?? InspeccionarCondicion(solicitud);
        if (rechazo is not null)
            return ResultadoDevolucion.Rechazo(rechazo);

        var reembolso = CalcularReembolso(solicitud.Pedido);        // paso 3
        var devolucion = RegistrarDevolucion(solicitud, reembolso); // paso 4
        var evento = GenerarEvento(devolucion);                     // paso 5

        return ResultadoDevolucion.Exito(devolucion, evento);
    }

    // ---- Pasos invariantes (iguales para todas las variantes) ----

    private string? VerificarElegibilidad(SolicitudDevolucion solicitud)
    {
        var pedido = solicitud.Pedido;

        if (pedido.Estado != EstadoPedido.Entregado)
            return "Solo se pueden devolver pedidos ENTREGADOS.";

        if (pedido.Devuelto)
            return "El pedido ya fue devuelto.";

        var dias = (solicitud.FechaSolicitudUtc - pedido.FechaEntregaUtc!.Value).TotalDays;

        if (dias < 0)
            return "La fecha de la solicitud es anterior a la entrega.";

        if (dias > PlazoMaximoDias)
            return $"Plazo vencido: {Motivo} admite hasta {PlazoMaximoDias} días y han pasado {(int)dias}.";

        return null;
    }

    private Devolucion RegistrarDevolucion(SolicitudDevolucion solicitud, Dinero reembolso)
    {
        var pedido = solicitud.Pedido;

        var devolucion = new Devolucion(
            pedido.Id, Motivo, reembolso, solicitud.FechaSolicitudUtc,
            pedido.ComponentesFisicosTotales());

        pedido.MarcarDevuelto(); // desde aquí el pedido no admite otra devolución
        return devolucion;
    }

    private static DevolucionAceptada GenerarEvento(Devolucion devolucion) =>
        new(devolucion.PedidoId, devolucion.Id, devolucion.Reembolso, devolucion.FechaUtc);

    // ---- Paso con valor por defecto que una variante PODRÍA personalizar (hook) ----

    // Reembolso = productos con descuento. La tarifa de envío NO se devuelve.
    protected virtual Dinero CalcularReembolso(Pedido pedido) => pedido.SubtotalConDescuento;
}