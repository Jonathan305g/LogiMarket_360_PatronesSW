namespace LogiMarket.Application.Abstractions.Gateways;

// Puerto SIMULADO: Infrastructure lo implementará con un colaborador real
// (por ejemplo, que escribe el correo a un archivo de log), nunca con un envío verdadero.
public interface ICanalEmail
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default);
}