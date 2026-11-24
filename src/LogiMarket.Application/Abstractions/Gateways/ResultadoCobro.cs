namespace LogiMarket.Application.Abstractions.Gateways;

public sealed class ResultadoCobro
{
    public bool Aprobado { get; }
    public bool Exitoso => Aprobado;
    public string? Referencia { get; }
    public string? MotivoRechazo { get; }

    private ResultadoCobro(bool aprobado, string? referencia, string? motivoRechazo)
    {
        Aprobado = aprobado;
        Referencia = referencia;
        MotivoRechazo = motivoRechazo;
    }

    public static ResultadoCobro Exito(string referencia)
    {
        if (string.IsNullOrWhiteSpace(referencia))
            throw new ArgumentException("Un cobro aprobado requiere referencia.", nameof(referencia));

        return new ResultadoCobro(true, referencia, null);
    }

    public static ResultadoCobro Rechazo(string motivo) => new(false, null, motivo);
}
