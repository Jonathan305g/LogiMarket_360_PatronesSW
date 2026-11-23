namespace LogiMarket.Application.Validation;

public sealed class ResultadoValidacion
{
    public ErrorValidacion? Error { get; }
    public bool Aprobada => Error is null;

    // Nombres de los eslabones que se ejecutaron, en orden: hace observable
    // dónde se detuvo la cadena y por qué.
    public IReadOnlyList<string> Traza { get; }

    private ResultadoValidacion(ErrorValidacion? error, IEnumerable<string> traza)
    {
        Error = error;
        Traza = traza.ToList();
    }

    public static ResultadoValidacion Aprobar(IEnumerable<string> traza) => new(null, traza);

    public static ResultadoValidacion Rechazar(ErrorValidacion error, IEnumerable<string> traza) =>
        new(error, traza);
} 