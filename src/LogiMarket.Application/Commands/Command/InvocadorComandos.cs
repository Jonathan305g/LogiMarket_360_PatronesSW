using LogiMarket.Application.Abstractions.Services;

namespace LogiMarket.Application.Commands.Command;

// El INVOCADOR: ejecuta cualquier comando y lo deja registrado. No sabe qué hace cada uno.
public sealed class InvocadorComandos
{
    private readonly IUsuarioActual _usuario;
    private readonly List<RegistroComando> _registro = new();

    public InvocadorComandos(IUsuarioActual usuario)
    {
        _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
    }

    // Solo lectura: nadie puede borrar ni alterar el historial desde fuera.
    public IReadOnlyList<RegistroComando> Registro => _registro;

    public async Task<TResultado> EjecutarAsync<TResultado>(
        IComando<TResultado> comando, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        try
        {
            var resultado = await comando.EjecutarAsync(ct);
            var resumen = comando.Resumir(resultado);
            Anotar(comando, resumen.Exitoso, resumen.Texto);
            return resultado;
        }
        catch (Exception ex)
        {
            // Una excepción también se registra, y después se vuelve a lanzar tal cual.
            Anotar(comando, false, $"EXCEPCIÓN {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    private void Anotar<TResultado>(IComando<TResultado> comando, bool exitoso, string resultado) =>
        _registro.Add(new RegistroComando(
            DateTime.UtcNow, comando.Nombre, comando.Entrada, _usuario.Nombre, exitoso, resultado));
}