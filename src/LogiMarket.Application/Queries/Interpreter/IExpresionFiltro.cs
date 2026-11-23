namespace LogiMarket.Application.Queries.Interpreter;

// Toda pieza del árbol —una comparación o un AND/OR— sabe hacer lo mismo: evaluarse.
public interface IExpresionFiltro
{
    bool Evaluar(PedidoConsulta pedido);
}