namespace LogiMarket.Application.Repositories;

public class ConflictoConcurrenciaException : Exception
{
    public ConflictoConcurrenciaException(string message)
        : base(message)
    {
    }

    public ConflictoConcurrenciaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}