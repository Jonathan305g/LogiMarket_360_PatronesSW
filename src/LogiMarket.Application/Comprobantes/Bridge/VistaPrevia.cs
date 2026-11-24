namespace LogiMarket.Application.Comprobantes.Bridge;

// Implementor 1: arma el texto en memoria, para mostrarlo en pantalla. No toca disco.
public class VistaPrevia : ISalidaDocumento
{
    public string Entregar(string titulo, IReadOnlyList<string> lineas)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(titulo);
        sb.AppendLine(new string('-', titulo.Length));
        foreach (var l in lineas) sb.AppendLine(l);
        return sb.ToString();
    }
}