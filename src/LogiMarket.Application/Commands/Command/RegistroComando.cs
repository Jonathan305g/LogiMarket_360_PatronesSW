namespace LogiMarket.Application.Commands.Command;

// Una línea del historial de ejecuciones.
public record RegistroComando(
    DateTime FechaUtc,
    string Comando,
    string Entrada,
    string Usuario,
    bool Exitoso,
    string Resultado);