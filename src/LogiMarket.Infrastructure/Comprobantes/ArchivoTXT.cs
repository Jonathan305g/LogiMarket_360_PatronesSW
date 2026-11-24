using LogiMarket.Application.Comprobantes.Bridge;

namespace LogiMarket.Infrastructure.Comprobantes;

// Implementor concreto que guarda el comprobante como archivo de texto.
public sealed class ArchivoTXT : ISalidaDocumento
{
    private readonly string _directorio;

    public ArchivoTXT(string directorio = "Comprobantes")
    {
        if (string.IsNullOrWhiteSpace(directorio))
            throw new ArgumentException("La carpeta de comprobantes es obligatoria.", nameof(directorio));

        _directorio = directorio;
    }

    public string Entregar(string titulo, IReadOnlyList<string> lineas)
    {
        ArgumentNullException.ThrowIfNull(titulo);
        ArgumentNullException.ThrowIfNull(lineas);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(titulo);
        sb.AppendLine(new string('-', titulo.Length));
        foreach (var linea in lineas)
            sb.AppendLine(linea);

        var contenido = sb.ToString();
        Directory.CreateDirectory(_directorio);
        var ruta = Path.Combine(_directorio, $"comprobante_{Guid.NewGuid():N}.txt");

        File.WriteAllText(ruta, contenido);
        return ruta;
    }
}
