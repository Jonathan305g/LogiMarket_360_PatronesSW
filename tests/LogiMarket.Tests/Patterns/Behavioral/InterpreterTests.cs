using LogiMarket.Application.Queries.Interpreter;

namespace LogiMarket.Tests.Patterns.Behavioral;

public class InterpreterTests
{
    private static List<PedidoConsulta> Muestra() => new()
    {
        new PedidoConsulta(Guid.NewGuid(), "PAGADO", 59.00m, "Ana"),
        new PedidoConsulta(Guid.NewGuid(), "PAGADO", 102.00m, "Luis"),
        new PedidoConsulta(Guid.NewGuid(), "PAGADO", 90.00m, "Carmen"),
        new PedidoConsulta(Guid.NewGuid(), "PAGADO", 30.00m, "Pedro"),     // PAGADO pero bajo el umbral
        new PedidoConsulta(Guid.NewGuid(), "BORRADOR", 200.00m, "Elena"), // total alto, pero NO pagado
        new PedidoConsulta(Guid.NewGuid(), "DESPACHADO", 150.00m, "Jose"),
    };

    private static List<string> Filtrar(string filtro) =>
        FiltroPedidos.Aplicar(filtro, Muestra()).Select(p => p.Cliente).OrderBy(n => n).ToList();

    [Fact]
    public void Aplicar_EstadoPagadoYTotalMayorOIgualA50_DevuelveSoloLosQueCumplen()   // P25
    {
        var resultado = Filtrar("ESTADO = PAGADO AND TOTAL >= 50");

        Assert.Equal(new[] { "Ana", "Carmen", "Luis" }, resultado); // Pedro (30) y los no pagados quedan fuera
    }

    [Fact]
    public void Aplicar_FiltroPorCliente_DevuelveSoloEsePedido()
    {
        var resultado = Filtrar("CLIENTE = Ana");

        Assert.Equal(new[] { "Ana" }, resultado);
    }

    [Fact]
    public void Aplicar_AndTieneMasPrecedenciaQueOr_SinParentesis()
    {
        // Se interpreta: ESTADO=PAGADO OR (ESTADO=DESPACHADO AND TOTAL>=100)
        var resultado = Filtrar("ESTADO = PAGADO OR ESTADO = DESPACHADO AND TOTAL >= 100");

        // Pedro entra por la rama PAGADO (el AND no le aplica); Jose entra porque 150>=100.
        Assert.Equal(new[] { "Ana", "Carmen", "Jose", "Luis", "Pedro" }, resultado);
    }

    [Fact]
    public void Aplicar_ParentesisCambianLaPrecedencia()
    {
        // Con paréntesis, el AND aplica a TODO el OR: (PAGADO o DESPACHADO) Y total >= 100.
        var resultado = Filtrar("(ESTADO = PAGADO OR ESTADO = DESPACHADO) AND TOTAL >= 100");

        Assert.Equal(new[] { "Jose", "Luis" }, resultado); // Ana(59), Carmen(90) y Pedro(30) ya no entran
    }

    [Theory]
    [InlineData("ESTADO =")]                  // falta el valor
    [InlineData("(ESTADO = PAGADO")]          // paréntesis sin cerrar
    [InlineData("ESTADO = PAGADO)")]          // paréntesis sobrante
    [InlineData("ESTADO >= PAGADO")]          // operador no válido para un campo de texto
    [InlineData("AND ESTADO = PAGADO")]       // empieza con un conector
    public void Aplicar_SintaxisInvalida_LanzaErrorLegibleSinEjecutarNada(string filtroInvalido)   // P26
    {
        Assert.Throws<InterpreterSyntaxException>(() => Filtrar(filtroInvalido));
    }

    [Fact]
    public void Aplicar_ValorNoNumericoParaTotal_LanzaErrorLegible()
    {
        Assert.Throws<InterpreterSyntaxException>(() => Filtrar("TOTAL >= ABC"));
    }

    [Fact]
    public void Aplicar_SinResultados_DevuelveListaVaciaSinError()
    {
        var resultado = Filtrar("ESTADO = ENTREGADO");

        Assert.Empty(resultado);
    }
}