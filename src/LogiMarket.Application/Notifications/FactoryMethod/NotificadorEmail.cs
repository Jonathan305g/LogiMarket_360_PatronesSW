using LogiMarket.Application.Abstractions.Gateways;

namespace LogiMarket.Application.Notifications.FactoryMethod;

public sealed class NotificadorEmail : INotificador
{
    private readonly ICanalEmail _canal;
    public NotificadorEmail(ICanalEmail canal) => _canal = canal;

    public string Canal => "EMAIL";

    public Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default) =>
        _canal.EnviarAsync(destinatario, asunto, cuerpo, ct);
}