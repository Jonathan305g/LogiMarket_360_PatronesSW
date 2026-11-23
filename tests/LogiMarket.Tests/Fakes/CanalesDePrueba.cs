using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Abstractions.Services;

namespace LogiMarket.Tests.Fakes;

public sealed class CanalEmailEspia : ICanalEmail
{
    public List<(string Destinatario, string Asunto, string Cuerpo)> Enviados { get; } = new();

    public Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct = default)
    {
        Enviados.Add((destinatario, asunto, cuerpo));
        return Task.CompletedTask;
    }
}

public sealed class CanalSmsEspia : ICanalSms
{
    public List<(string Destinatario, string Mensaje)> Enviados { get; } = new();

    public Task EnviarAsync(string destinatario, string mensaje, CancellationToken ct = default)
    {
        Enviados.Add((destinatario, mensaje));
        return Task.CompletedTask;
    }
}

public sealed class BitacoraAuditoriaEspia : IBitacoraAuditoria
{
    public List<(string TipoEvento, string Detalle, DateTime OcurrioUtc)> Registros { get; } = new();

    public void Registrar(string tipoEvento, string detalle, DateTime ocurrioUtc) =>
        Registros.Add((tipoEvento, detalle, ocurrioUtc));
}