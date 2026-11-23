namespace LogiMarket.Application.Queries.Interpreter;

// El PARSER: convierte la lista de tokens en un árbol de IExpresionFiltro,
// respetando la precedencia (AND liga más fuerte que OR) y los paréntesis.
public sealed class AnalizadorFiltro
{
    private readonly List<Token> _tokens;
    private int _pos;

    private AnalizadorFiltro(List<Token> tokens) => _tokens = tokens;

    public static IExpresionFiltro Analizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new InterpreterSyntaxException("El filtro no puede estar vacío.");

        var analizador = new AnalizadorFiltro(Tokenizador.Tokenizar(texto));
        var expresion = analizador.AnalizarO();
        analizador.Esperar(TipoToken.Fin); // si sobran tokens, el filtro está mal formado
        return expresion;
    }

    // OR es lo ÚLTIMO que se evalúa (menor precedencia): se construye primero, "por fuera".
    private IExpresionFiltro AnalizarO()
    {
        var izquierda = AnalizarY();
        while (Actual.Tipo == TipoToken.O)
        {
            Avanzar();
            izquierda = new ExpresionO(izquierda, AnalizarY());
        }
        return izquierda;
    }

    // AND liga MÁS que OR: se resuelve primero, "por dentro".
    private IExpresionFiltro AnalizarY()
    {
        var izquierda = AnalizarPrimario();
        while (Actual.Tipo == TipoToken.Y)
        {
            Avanzar();
            izquierda = new ExpresionY(izquierda, AnalizarPrimario());
        }
        return izquierda;
    }

    private IExpresionFiltro AnalizarPrimario()
    {
        if (Actual.Tipo == TipoToken.ParentesisIzq)
        {
            Avanzar();
            var expresion = AnalizarO(); // dentro del paréntesis se reinicia la precedencia
            Esperar(TipoToken.ParentesisDer);
            return expresion;
        }

        return AnalizarComparacion();
    }

    private IExpresionFiltro AnalizarComparacion()
    {
        var campo = Esperar(TipoToken.Campo).Texto;
        var operador = Esperar(TipoToken.Operador).Texto;
        var valor = Esperar(TipoToken.Valor).Texto;
        return new ExpresionComparacion(campo, operador, valor);
    }

    private Token Actual => _tokens[_pos];
    private void Avanzar() => _pos++;

    private Token Esperar(TipoToken tipo)
    {
        if (Actual.Tipo != tipo)
            throw new InterpreterSyntaxException(
                $"Se esperaba {tipo} y se encontró '{Actual.Texto}' en la posición {_pos}.");

        var token = Actual;
        Avanzar();
        return token;
    }
}