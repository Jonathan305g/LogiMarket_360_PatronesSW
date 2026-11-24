using System.Diagnostics;
using LogiMarket.Application.Abstractions.Gateways;

namespace LogiMarket.Infrastructure.Notificaciones;

public sealed class CanalEmailSimulado : ICanalEmail
{
    public Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Trace.WriteLine($"[EMAIL SIMULADO] Para: {destinatario}; Asunto: {asunto}; {cuerpo}");
        return Task.CompletedTask;
    }
}
