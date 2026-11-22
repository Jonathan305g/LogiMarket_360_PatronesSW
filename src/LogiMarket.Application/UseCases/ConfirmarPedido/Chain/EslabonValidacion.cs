using LogiMarket.Application.Validation;

namespace LogiMarket.Application.UseCases.ConfirmarPedido.Chain;

public abstract class EslabonValidacion
{
    private EslabonValidacion? _siguiente;

    // Enlaza el siguiente eslabón y lo devuelve, para poder encadenar: a.Siguiente(b).Siguiente(c)
    public EslabonValidacion Siguiente(EslabonValidacion siguiente)
    {
        _siguiente = siguiente;
        return siguiente;
    }

    public ResultadoValidacion Validar(ContextoConfirmacion contexto) =>
        Procesar(contexto, new List<string>());

    private ResultadoValidacion Procesar(ContextoConfirmacion contexto, List<string> traza)
    {
        traza.Add(GetType().Name);

        // Cada eslabón decide: rechazar (y la cadena se detiene aquí)...
        var error = Evaluar(contexto);
        if (error is not null)
            return ResultadoValidacion.Rechazar(error, traza);

        // ...o delegar en el siguiente. Si no hay más, todo salió bien.
        return _siguiente is null
            ? ResultadoValidacion.Aprobar(traza)
            : _siguiente.Procesar(contexto, traza);
    }

    // Lo único que cada eslabón concreto define: null = pasa, o el error que lo detiene.
    protected abstract ErrorValidacion? Evaluar(ContextoConfirmacion contexto);
}