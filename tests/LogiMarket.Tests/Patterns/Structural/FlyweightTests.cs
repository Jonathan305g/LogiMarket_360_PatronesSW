using LogiMarket.Domain.Catalog.Flyweight;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.ValueObjects;
using Xunit.Abstractions;

namespace LogiMarket.Tests.Patterns.Structural;

public class FlyweightTests
{
    private const int Filas = 1000;

    // Los 5 SKU oficiales de la guía.
    private static readonly (string Sku, string Nombre, string Categoria, string Descripcion, decimal Precio)[] Datos =
    {
        ("P01", "Mouse USB", "Periféricos", "Mouse óptico USB de 1200 DPI", 20m),
        ("P02", "Teclado", "Periféricos", "Teclado estándar USB en español", 30m),
        ("P03", "Memoria USB", "Almacenamiento", "Memoria flash USB 3.0", 10m),
        ("P04", "Webcam", "Video", "Cámara web HD con micrófono", 40m),
        ("P05", "Alfombrilla", "Accesorios", "Alfombrilla antideslizante para mouse", 5m),
    };

    private readonly ITestOutputHelper _salida;

    public FlyweightTests(ITestOutputHelper salida) => _salida = salida;

    // Devuelve un texto NUEVO en memoria cada vez (como cuando se lee de la base de datos):
    // así ambos escenarios parten de cadenas realmente distintas.
    private static string Nueva(string texto) => new string(texto.AsSpan());

    // ----- Escenario CON Flyweight -----
    private static List<FilaCatalogoVisual> ConstruirCompartidas(CatalogoVisualFlyweightFactory fabrica)
    {
        var filas = new List<FilaCatalogoVisual>(Filas);
        for (var i = 0; i < Filas; i++)
        {
            var d = Datos[i % Datos.Length];
            var metadatos = fabrica.Obtener(Nueva(d.Sku), Nueva(d.Nombre), Nueva(d.Categoria), Nueva(d.Descripcion));
            filas.Add(new FilaCatalogoVisual(metadatos, Stock: i, Cantidad: 1, PrecioVenta: new Dinero(d.Precio)));
        }
        return filas;
    }

    // ----- Escenario de CONTROL: mismos datos, pero cada fila con sus propios metadatos -----
    private sealed class MetadatosCopia
    {
        public MetadatosCopia(string sku, string nombre, string categoria, string descripcion)
        {
            Sku = sku; Nombre = nombre; Categoria = categoria; Descripcion = descripcion;
        }

        public string Sku { get; }
        public string Nombre { get; }
        public string Categoria { get; }
        public string Descripcion { get; }
    }

    private sealed record FilaCopia(MetadatosCopia Metadatos, int Stock, int Cantidad, Dinero PrecioVenta);

    private static List<FilaCopia> ConstruirSinCompartir()
    {
        var filas = new List<FilaCopia>(Filas);
        for (var i = 0; i < Filas; i++)
        {
            var d = Datos[i % Datos.Length];
            var metadatos = new MetadatosCopia(Nueva(d.Sku), Nueva(d.Nombre), Nueva(d.Categoria), Nueva(d.Descripcion));
            filas.Add(new FilaCopia(metadatos, Stock: i, Cantidad: 1, PrecioVenta: new Dinero(d.Precio)));
        }
        return filas;
    }

    // Mide cuánta memoria queda retenida por lo que construye, forzando el recolector antes y después.
    private static long Medir(Func<object> construir)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var antes = GC.GetTotalMemory(forceFullCollection: true);
        var resultado = construir();
        var despues = GC.GetTotalMemory(forceFullCollection: true);

        GC.KeepAlive(resultado); // evita que se libere antes de medir
        return despues - antes;
    }

    [Fact]
    public void Obtener_MismoSku_DevuelveLaMismaInstancia()
    {
        var fabrica = new CatalogoVisualFlyweightFactory();

        var a = fabrica.Obtener("P01", "Mouse USB", "Periféricos", "Mouse óptico");
        var b = fabrica.Obtener("P01", Nueva("Mouse USB"), "Periféricos", "Mouse óptico");
        var c = fabrica.Obtener("P02", "Teclado", "Periféricos", "Teclado USB");

        Assert.True(ReferenceEquals(a, b));
        Assert.False(ReferenceEquals(a, c));
        Assert.Equal(2, fabrica.InstanciasCreadas);
    }

    [Fact]
    public void MilFilas_CompartenSoloCincoMetadatos_YCadaFilaConservaSusDatos()
    {
        var fabrica = new CatalogoVisualFlyweightFactory();

        var filas = ConstruirCompartidas(fabrica);

        Assert.Equal(Filas, filas.Count);
        Assert.Equal(5, fabrica.InstanciasCreadas);
        Assert.Equal(5, filas.Select(f => f.Metadatos).Distinct(ReferenceEqualityComparer.Instance).Count());

        // Filas 0 y 5 son del mismo SKU: comparten metadatos...
        Assert.True(ReferenceEquals(filas[0].Metadatos, filas[5].Metadatos));
        // ...pero sus datos externos son propios.
        Assert.Equal(0, filas[0].Stock);
        Assert.Equal(5, filas[5].Stock);
    }

    [Fact]
    public void Memoria_ConFlyweight_EsMenorQueSinCompartirConLosMismosDatos()
    {
        var compartido = Medir(() => ConstruirCompartidas(new CatalogoVisualFlyweightFactory()));
        var sinCompartir = Medir(() => ConstruirSinCompartir());

        // Evidencia para el informe: condiciones y resultado.
        _salida.WriteLine($"Condiciones: {Filas} filas, {Datos.Length} SKU distintos, cadenas nuevas por fila en ambos escenarios, GC forzado antes y después.");
        _salida.WriteLine($"Con Flyweight : {Datos.Length} objetos de metadatos, {compartido:N0} bytes retenidos.");
        _salida.WriteLine($"Sin compartir : {Filas} objetos de metadatos, {sinCompartir:N0} bytes retenidos.");

        Assert.True(compartido < sinCompartir,
            $"Con Flyweight: {compartido} B; sin compartir: {sinCompartir} B.");
    }

    [Fact]
    public void Obtener_SkuVacio_LanzaDomainException()
    {
        var fabrica = new CatalogoVisualFlyweightFactory();

        Assert.Throws<DomainException>(() => fabrica.Obtener("  ", "X", "Y", "Z"));
    }
}