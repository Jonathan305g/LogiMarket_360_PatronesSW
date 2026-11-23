namespace LogiMarket.Application.Abstractions.Repositories;

// La base detectó que otro proceso modificó los mismos datos (control optimista).
public class ConflictoConcurrenciaException : Exception
{
    public ConflictoConcurrenciaException() : base("Los datos cambiaron mientras se confirmaba la operación.")
    {
    }
}