using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Events;

namespace LogiMarket.Domain.Returns.TemplateMethod;

public sealed class ResultadoDevolucion
{
    public bool Aceptada { get; }
    public string? MotivoRechazo { get; }
    public Devolucion? Devolucion { get; }
    public DevolucionAceptada? Evento { get; }

    private ResultadoDevolucion(bool aceptada, string? motivoRechazo,
        Devolucion? devolucion, DevolucionAceptada? evento)
    {
        Aceptada = aceptada;
        MotivoRechazo = motivoRechazo;
        Devolucion = devolucion;
        Evento = evento;
    }

    public static ResultadoDevolucion Exito(Devolucion devolucion, DevolucionAceptada evento) =>
        new(true, null, devolucion, evento);

    public static ResultadoDevolucion Rechazo(string motivo) =>
        new(false, motivo, null, null);
}