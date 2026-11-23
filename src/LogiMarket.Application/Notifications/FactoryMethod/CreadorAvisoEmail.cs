using LogiMarket.Application.Abstractions.Gateways;

namespace LogiMarket.Application.Notifications.FactoryMethod;

public sealed class CreadorAvisoEmail : CreadorAviso
{
    private readonly ICanalEmail _canal;
    public CreadorAvisoEmail(ICanalEmail canal) => _canal = canal;

    protected override INotificador CrearNotificador() => new NotificadorEmail(_canal);
}