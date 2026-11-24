using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class ClienteRepositorioEf(LogiMarketDbContext db) : IClienteRepository
{
    public Task<Cliente?> ObtenerPorIdAsync(Guid id) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IReadOnlyList<Cliente>> ListarAsync() =>
        await db.Clientes.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();

    public void Agregar(Cliente cliente) => db.Clientes.Add(cliente);
}
