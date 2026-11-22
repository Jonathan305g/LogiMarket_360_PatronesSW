using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Returns.TemplateMethod;

public sealed class DevolucionPorArrepentimiento : ProcesarDevolucion
{
    protected override MotivoDevolucion Motivo => MotivoDevolucion.Arrepentimiento;
    protected override int PlazoMaximoDias => 7;

    // El arrepentimiento exige que el producto esté sin uso.
    protected override string? InspeccionarCondicion(SolicitudDevolucion solicitud) =>
        solicitud.ProductoSinUso
            ? null
            : "El arrepentimiento solo procede si el producto está sin uso.";
}