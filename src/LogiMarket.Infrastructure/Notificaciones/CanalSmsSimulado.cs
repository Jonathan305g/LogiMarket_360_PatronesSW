using System.Diagnostics;
using LogiMarket.Application.Abstractions.Gateways;

namespace LogiMarket.Infrastructure.Notificaciones;

public sealed class CanalSmsSimulado : ICanalSms
{
    public Task EnviarAsync(string destinatario, string mensaje, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Trace.WriteLine($"[SMS SIMULADO] Para: {destinatario}; {mensaje}");
        return Task.CompletedTask;
    }
}
