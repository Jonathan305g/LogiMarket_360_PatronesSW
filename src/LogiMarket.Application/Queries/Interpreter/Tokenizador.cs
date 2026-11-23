namespace LogiMarket.Application.Queries.Interpreter;

// Convierte el texto del filtro en una lista de Token, de izquierda a derecha.
public static class Tokenizador
{
    private static readonly HashSet<string> Campos =
        new(StringComparer.OrdinalIgnoreCase) { "ESTADO", "TOTAL", "CLIENTE" };

    public static List<Token> Tokenizar(string texto)
    {
        var tokens = new List<Token>();
        var i = 0;

        while (i < texto.Length)
        {
            var c = texto[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (c == '(') { tokens.Add(new Token(TipoToken.ParentesisIzq, "(")); i++; continue; }
            if (c == ')') { tokens.Add(new Token(TipoToken.ParentesisDer, ")")); i++; continue; }

            // Operadores de DOS caracteres primero (>=, <=, !=), o la gramática los leería mal.
            if (i + 1 < texto.Length && (c is '>' or '<' or '!') && texto[i + 1] == '=')
            {
                tokens.Add(new Token(TipoToken.Operador, texto.Substring(i, 2)));
                i += 2;
                continue;
            }
            if (c is '=' or '>' or '<')
            {
                tokens.Add(new Token(TipoToken.Operador, c.ToString()));
                i++;
                continue;
            }

            // Texto entre comillas simples: CLIENTE = 'Ana Pérez' (nombres con espacios).
            if (c == '\'')
            {
                var fin = texto.IndexOf('\'', i + 1);
                if (fin < 0)
                    throw new InterpreterSyntaxException($"Cadena sin cerrar en la posición {i}.");

                tokens.Add(new Token(TipoToken.Valor, texto.Substring(i + 1, fin - i - 1)));
                i = fin + 1;
                continue;
            }

            // Una palabra o número: AND, OR, un campo, PAGADO, 50, Ana...
            if (char.IsLetterOrDigit(c) || c is '_' or '.')
            {
                var inicio = i;
                while (i < texto.Length && (char.IsLetterOrDigit(texto[i]) || texto[i] is '_' or '.'))
                    i++;

                var palabra = texto[inicio..i];

                tokens.Add(palabra.ToUpperInvariant() switch
                {
                    "AND" => new Token(TipoToken.Y, palabra),
                    "OR" => new Token(TipoToken.O, palabra),
                    _ when Campos.Contains(palabra) => new Token(TipoToken.Campo, palabra.ToUpperInvariant()),
                    _ => new Token(TipoToken.Valor, palabra)
                });
                continue;
            }

            throw new InterpreterSyntaxException($"Carácter no reconocido '{c}' en la posición {i}.");
        }

        tokens.Add(new Token(TipoToken.Fin, string.Empty));
        return tokens;
    }
}