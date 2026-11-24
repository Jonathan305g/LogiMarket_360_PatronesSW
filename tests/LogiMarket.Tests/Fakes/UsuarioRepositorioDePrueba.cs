using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Tests.Fakes;

public sealed class UsuarioRepositorioDePrueba : IUsuarioRepository
{
    private readonly Dictionary<string, Usuario> _usuarios = new(StringComparer.OrdinalIgnoreCase);

    public void Sembrar(Usuario usuario) => _usuarios[usuario.NombreUsuario] = usuario;

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_usuarios.GetValueOrDefault(nombreUsuario));
    }
}
