using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Pricing.Strategy;

// Qué política ganó y cuánto descuenta.
public record ResultadoDescuento(string Politica, Dinero Monto);