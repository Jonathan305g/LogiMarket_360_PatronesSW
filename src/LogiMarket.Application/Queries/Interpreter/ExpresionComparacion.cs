using System.Globalization;

namespace LogiMarket.Application.Queries.Interpreter;

// Expresión TERMINAL: una comparación de hoja, como "ESTADO = PAGADO" o "TOTAL >= 50".
public sealed class ExpresionComparacion : IExpresionFiltro
{
    private readonly string _campo;
    private readonly string _operador;
    private readonly string _valorTexto;

    public ExpresionComparacion(string campo, string operador, string valorTexto)
    {
        _campo = campo;
        _operador = operador;
        _valorTexto = valorTexto;
    }

    public bool Evaluar(PedidoConsulta pedido) => _campo switch
    {
        "ESTADO" => CompararTexto(pedido.Estado),
        "CLIENTE" => CompararTexto(pedido.Cliente),
        "TOTAL" => CompararNumero(pedido.Total),
        _ => throw new InterpreterSyntaxException($"Campo no soportado: '{_campo}'.")
    };

    private bool CompararTexto(string valorReal)
    {
        if (_operador is not ("=" or "!="))
            throw new InterpreterSyntaxException($"El campo '{_campo}' solo admite '=' o '!='.");

        var iguales = string.Equals(valorReal, _valorTexto, StringComparison.OrdinalIgnoreCase);
        return _operador == "=" ? iguales : !iguales;
    }

    private bool CompararNumero(decimal valorReal)
    {
        if (!decimal.TryParse(_valorTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valorEsperado))
            throw new InterpreterSyntaxException($"'{_valorTexto}' no es un número válido para TOTAL.");

        return _operador switch
        {
            "=" => valorReal == valorEsperado,
            "!=" => valorReal != valorEsperado,
            ">" => valorReal > valorEsperado,
            ">=" => valorReal >= valorEsperado,
            "<" => valorReal < valorEsperado,
            "<=" => valorReal <= valorEsperado,
            _ => throw new InterpreterSyntaxException($"Operador no soportado para TOTAL: '{_operador}'.")
        };
    }
}