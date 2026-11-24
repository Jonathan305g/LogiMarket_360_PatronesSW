using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class UsuarioRepositorioEf(LogiMarketDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default)
    {
        var normalizado = nombreUsuario.Trim().ToUpper();
        return db.Usuarios.FirstOrDefaultAsync(
            u => u.NombreUsuario.ToUpper() == normalizado && u.Activo, ct);
    }
}
