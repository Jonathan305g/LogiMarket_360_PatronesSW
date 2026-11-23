namespace LogiMarket.Application.Notifications.FactoryMethod;

// El CREADOR: define el PASO que usa el producto, pero no decide cuál producto construir.
public abstract class CreadorAviso
{
    // MÉTODO DE FÁBRICA: cada creador concreto decide qué INotificador entregar.
    protected abstract INotificador CrearNotificador();

    // Devuelve el canal usado, para que quien llama (el Observer) lo deje como evidencia.
    public async Task<string> NotificarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default)
    {
        var notificador = CrearNotificador();
        await notificador.EnviarAsync(destinatario, asunto, cuerpo, ct);
        return notificador.Canal;
    }
}