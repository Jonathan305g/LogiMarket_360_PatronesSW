namespace LogiMarket.Domain.Events;

// Marca común de "algo importante pasó en el negocio".
// Más adelante lo usarán Observer y Outbox.
public interface IEventoDominio
{
    DateTime OcurrioUtc { get; }
}