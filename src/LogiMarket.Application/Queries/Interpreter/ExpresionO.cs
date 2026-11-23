namespace LogiMarket.Application.Queries.Interpreter;

public sealed class ExpresionO : IExpresionFiltro
{
    private readonly IExpresionFiltro _izquierda;
    private readonly IExpresionFiltro _derecha;

    public ExpresionO(IExpresionFiltro izquierda, IExpresionFiltro derecha)
    {
        _izquierda = izquierda;
        _derecha = derecha;
    }

    public bool Evaluar(PedidoConsulta pedido) => _izquierda.Evaluar(pedido) || _derecha.Evaluar(pedido);
}