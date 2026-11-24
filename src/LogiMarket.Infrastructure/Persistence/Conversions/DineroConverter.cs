using LogiMarket.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LogiMarket.Infrastructure.Persistence.Conversions;

// Le enseña a EF Core a guardar un Dinero como un decimal y reconstruirlo al leer.
public sealed class DineroConverter : ValueConverter<Dinero, decimal>
{
    public DineroConverter() : base(d => d.Valor, v => new Dinero(v))
    {
    }
} 