namespace LogiMarket.Application.Commands.Command;

// El contrato de toda acción convertida en objeto.
public interface IComando<TResultado>
{
    string Nombre { get; }

    // Resumen legible de los datos con los que se ejecuta (para el registro).
    string Entrada { get; }

    Task<TResultado> EjecutarAsync(CancellationToken ct = default);

    // El comando sabe leer su propio resultado y resumirlo.
    ResumenResultado Resumir(TResultado resultado);
}