using LogiMarket.Application.Abstractions.Services;

namespace LogiMarket.Tests.Fakes;

public class BitacoraDePrueba : IBitacoraAuditoria
{
    public List<(string Tipo, string Detalle)> Registros { get; } = new();

    public Task RegistrarAsync(string tipoEvento, string detalle, CancellationToken ct = default)
    {
        Registros.Add((tipoEvento, detalle));
        return Task.CompletedTask;
    }
}