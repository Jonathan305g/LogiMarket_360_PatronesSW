using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Pricing.Strategy;

public class CalculadoraDescuento
{
    private readonly IReadOnlyList<IPoliticaDescuento> _politicas;

    // Recibe las políticas por constructor (DIP) y las ordena de mayor a menor prioridad.
    public CalculadoraDescuento(IEnumerable<IPoliticaDescuento> politicas)
    {
        ArgumentNullException.ThrowIfNull(politicas);
        _politicas = politicas.OrderByDescending(p => p.Prioridad).ToList();
    }

    public ResultadoDescuento Calcular(Dinero subtotal, ContextoDescuento contexto)
    {
        // Toma la PRIMERA política que aplica: como están ordenadas por prioridad,
        // gana la más prioritaria y nunca se suman dos (sin acumulación).
        var politica = _politicas.FirstOrDefault(p => p.Aplica(contexto))
            ?? throw new DomainException("No hay ninguna política de descuento aplicable.");

        // Dinero * decimal redondea a 2 decimales (AwayFromZero), como pide la guía.
        return new ResultadoDescuento(politica.Nombre, subtotal * politica.Tasa);
    }
}