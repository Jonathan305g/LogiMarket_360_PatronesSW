using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Returns.TemplateMethod;

public sealed class DevolucionPorDefecto : ProcesarDevolucion
{
    protected override MotivoDevolucion Motivo => MotivoDevolucion.Defecto;
    protected override int PlazoMaximoDias => 30;

    // Un defecto se acepta aunque el producto se haya usado (así se descubrió el defecto).
    protected override string? InspeccionarCondicion(SolicitudDevolucion solicitud) => null;
}