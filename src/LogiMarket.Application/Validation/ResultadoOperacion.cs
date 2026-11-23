namespace LogiMarket.Application.Validation;

// Resultado simple de una operación que no devuelve datos: salió bien, o falló con un error estructurado.
public sealed class ResultadoOperacion
{
    public bool Exitoso => Error is null;
    public ErrorValidacion? Error { get; }

    private ResultadoOperacion(ErrorValidacion? error) => Error = error;

    public static ResultadoOperacion Ok() => new(null);

    public static ResultadoOperacion Fallo(string codigo, string mensaje) =>
        new(new ErrorValidacion(codigo, mensaje));
}