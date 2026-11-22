using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Pricing.Strategy;

// Los datos que una política necesita para decidir si aplica.
public record ContextoDescuento(CategoriaCliente Categoria, int UnidadesFisicas);