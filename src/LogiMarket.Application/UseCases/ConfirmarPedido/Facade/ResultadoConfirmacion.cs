using LogiMarket.Application.Validation;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.UseCases.ConfirmarPedido.Facade;

public sealed class ResultadoConfirmacion
{
    public bool Exitoso { get; }

    // true = la clave ya se había usado y se devuelve el resultado ORIGINAL, sin cobrar de nuevo.
    public bool EsRepeticion { get; }

    public ErrorValidacion? Error { get; }
    public Pago? Pago { get; }

    private ResultadoConfirmacion(bool exitoso, bool esRepeticion, ErrorValidacion? error, Pago? pago)
    {
        Exitoso = exitoso;
        EsRepeticion = esRepeticion;
        Error = error;
        Pago = pago;
    }

    public static ResultadoConfirmacion Confirmado(Pago pago) => new(true, false, null, pago);
    public static ResultadoConfirmacion Repetido(Pago pago) => new(true, true, null, pago);
    public static ResultadoConfirmacion Rechazado(ErrorValidacion error) => new(false, false, error, null);
}