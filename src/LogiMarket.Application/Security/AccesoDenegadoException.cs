namespace LogiMarket.Application.Security;

// Se lanza cuando un usuario intenta una operación que su rol no permite
public class AccesoDenegadoException : Exception
{
    public AccesoDenegadoException(string mensaje) : base(mensaje)
    {
    }
}