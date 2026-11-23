using LogiMarket.Application.Validation;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.UseCases.Devoluciones;

public sealed class ResultadoDevolucionAplicacion
{
    public bool Exitoso => Error is null;
    public ErrorValidacion? Error { get; }
    public Devolucion? Devolucion { get; }

    private ResultadoDevolucionAplicacion(ErrorValidacion? error, Devolucion? devolucion)
    {
        Error = error;
        Devolucion = devolucion;
    }

    public static ResultadoDevolucionAplicacion Ok(Devolucion devolucion) => new(null, devolucion);
    public static ResultadoDevolucionAplicacion Fallo(string codigo, string mensaje) => new(new ErrorValidacion(codigo, mensaje), null); 
}