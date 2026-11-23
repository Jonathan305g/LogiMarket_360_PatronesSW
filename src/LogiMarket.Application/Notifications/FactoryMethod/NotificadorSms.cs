using LogiMarket.Application.Abstractions.Gateways;

namespace LogiMarket.Application.Notifications.FactoryMethod;

public sealed class NotificadorSms : INotificador
{
    private readonly ICanalSms _canal;
    public NotificadorSms(ICanalSms canal) => _canal = canal;

    public string Canal => "SMS";

    // El SMS no tiene asunto: si viene vacío, se envía solo el cuerpo; si no, se antepone.
    public Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default) =>
        _canal.EnviarAsync(destinatario, string.IsNullOrWhiteSpace(asunto) ? cuerpo : $"{asunto}: {cuerpo}", ct);
}