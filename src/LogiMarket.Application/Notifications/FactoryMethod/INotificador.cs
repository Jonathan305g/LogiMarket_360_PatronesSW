namespace LogiMarket.Application.Notifications.FactoryMethod;

// Contrato COMÚN para cualquier forma de aviso. Quien lo usa no sabe si es EMAIL o SMS.
public interface INotificador
{
    string Canal { get; }
    Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default);
}