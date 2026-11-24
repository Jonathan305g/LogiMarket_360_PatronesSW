using System.Text.Json;
using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Events;
using LogiMarket.Infrastructure.Persistence.Models;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class OutboxEf(LogiMarketDbContext db) : IOutbox
{
    public void Registrar(IEventoDominio evento)
    {
        ArgumentNullException.ThrowIfNull(evento);
        var tipo = evento.GetType();
        db.EventosOutbox.Add(new EventoOutboxRegistro
        {
            Id = Guid.NewGuid(),
            Tipo = tipo.Name,
            DatosJson = JsonSerializer.Serialize(evento, tipo),
            OcurrioUtc = evento.OcurrioUtc
        });
    }
}
