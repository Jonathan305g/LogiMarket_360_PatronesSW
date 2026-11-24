using LogiMarket.Application.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWorkEf(LogiMarketDbContext db) : IUnitOfWork
{
    public async Task CommitAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoConcurrenciaException();
        }
    }
}
