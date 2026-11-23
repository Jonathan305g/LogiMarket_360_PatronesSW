using LogiMarket.Application.Abstractions.Gateways;

namespace LogiMarket.Application.Notifications.FactoryMethod;

public sealed class CreadorAvisoSms : CreadorAviso
{
    private readonly ICanalSms _canal;
    public CreadorAvisoSms(ICanalSms canal) => _canal = canal;

    protected override INotificador CrearNotificador() => new NotificadorSms(_canal);
}