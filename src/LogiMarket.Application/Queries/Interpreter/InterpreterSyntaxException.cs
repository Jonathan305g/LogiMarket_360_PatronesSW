namespace LogiMarket.Application.Queries.Interpreter;

// Error de sintaxis (o de valor) en un filtro de texto. Legible para el usuario.
public sealed class InterpreterSyntaxException : Exception
{
    public InterpreterSyntaxException(string mensaje) : base(mensaje)
    {
    }
}