namespace LogiMarket.Application.Queries.Interpreter;

// Punto de entrada para quien consulta: arma el árbol UNA vez y lo aplica a la colección.
public static class FiltroPedidos
{
    public static IEnumerable<PedidoConsulta> Aplicar(string filtro, IEnumerable<PedidoConsulta> pedidos)
    {
        var expresion = AnalizadorFiltro.Analizar(filtro);
        return pedidos.Where(expresion.Evaluar);
    }
}