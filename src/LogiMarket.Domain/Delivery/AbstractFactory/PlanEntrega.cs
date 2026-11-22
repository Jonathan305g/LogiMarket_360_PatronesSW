using LogiMarket.Domain.Enums;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Delivery.AbstractFactory;

// Resultado de planificar una entrega: todo lo que sale de UNA familia.
public record PlanEntrega(
    TipoEntrega Tipo,
    Dinero Tarifa,
    int DiasEstimados,
    DateTime FechaEstimadaUtc,
    string Etiqueta);