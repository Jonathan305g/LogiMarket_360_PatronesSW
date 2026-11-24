using LogiMarket.Application.Comprobantes.Bridge;

namespace LogiMarket.Infrastructure.Comprobantes;

// Implementor concreto que guarda el comprobante como archivo de texto.
public sealed class ArchivoTXT : ISalidaDocumento
{
    private readonly string _ruta;

    public ArchivoTXT(string ruta = "comprobante.txt")
    {
        if (string.IsNullOrWhiteSpace(ruta))
            throw new ArgumentException("La ruta del comprobante es obligatoria.", nameof(ruta));

        _ruta = ruta;
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
        var directorio = Path.GetDirectoryName(_ruta);
        if (!string.IsNullOrEmpty(directorio))
            Directory.CreateDirectory(directorio);

        File.WriteAllText(_ruta, contenido);
        return contenido;
    }
}
