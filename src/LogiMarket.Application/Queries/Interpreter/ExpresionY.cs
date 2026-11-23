namespace LogiMarket.Application.Queries.Interpreter;

// Expresión NO terminal: combina dos sub-árboles con AND.
public sealed class ExpresionY : IExpresionFiltro
{
    private readonly IExpresionFiltro _izquierda;
    private readonly IExpresionFiltro _derecha;

    public ExpresionY(IExpresionFiltro izquierda, IExpresionFiltro derecha)
    {
        _izquierda = izquierda;
        _derecha = derecha;
    }

    public bool Evaluar(PedidoConsulta pedido) => _izquierda.Evaluar(pedido) && _derecha.Evaluar(pedido);
}