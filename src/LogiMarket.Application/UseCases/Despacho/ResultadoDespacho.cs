using LogiMarket.Application.Validation;

namespace LogiMarket.Application.UseCases.Despacho;

public sealed class ResultadoDespacho
{
    public bool Exitoso => Error is null;
    public ErrorValidacion? Error { get; }
    public string? Guia { get; }

    private ResultadoDespacho(ErrorValidacion? error, string? guia)
    {
        Error = error;
        Guia = guia;
    }

    public static ResultadoDespacho Ok(string guia) => new(null, guia);
    public static ResultadoDespacho Fallo(string codigo, string mensaje) => new(new ErrorValidacion(codigo, mensaje), null);
}