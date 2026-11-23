namespace LogiMarket.Application.Abstractions.Gateways;

// Puerto: lo que Application necesita de "una pasarela de pago". No sabe quién la implementa.
public interface IPasarelaPago
{
    // La clave de idempotencia evita cobrar dos veces el mismo pedido.
    Task<ResultadoCobro> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default);
}