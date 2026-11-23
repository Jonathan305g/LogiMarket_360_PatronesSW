namespace LogiMarket.Application.Abstractions.Gateways;

// Lo que devuelve cualquier pasarela de pago, sea cual sea el proveedor.
public record ResultadoCobro(bool Aprobado, string? Referencia, string? MotivoRechazo)
{
    public static ResultadoCobro Exito(string referencia) => new(true, referencia, null);

    public static ResultadoCobro Rechazo(string motivo) => new(false, null, motivo);
}