namespace LogiMarket.Application.Abstractions.Gateways;

public interface ICanalSms
{
    Task EnviarAsync(string destinatario, string mensaje, CancellationToken ct = default);
}