using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Domain.Entities;

namespace LogiMarket.Tests.Architecture;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void DomainAndApplication_DoNotReferenceInfrastructureOrEfCore() // P38 (DIP)
    {
        AssertNoForbiddenReferences(typeof(Pedido).Assembly, "Infrastructure", "EntityFrameworkCore", "Sqlite", "WinForms");
        AssertNoForbiddenReferences(typeof(IPasarelaPago).Assembly, "Infrastructure", "EntityFrameworkCore", "Sqlite", "WinForms");
    }

    [Fact]
    public void Domain_DoesNotReferenceApplication()
    {
        AssertNoForbiddenReferences(typeof(Pedido).Assembly, "Application");
    }

    [Fact]
    public void PaymentContracts_AreSegregatedByCapability() // P38 (ISP)
    {
        Assert.Equal(new[] { "CobrarAsync" }, typeof(IPasarelaPago).GetMethods().Select(m => m.Name));
        Assert.Equal(new[] { "RechazarSiguienteCobro" }, typeof(IPasarelaPagoDemo).GetMethods().Select(m => m.Name));
        Assert.DoesNotContain(typeof(IPasarelaPago).GetMethods(), m => m.Name.Contains("Rechazar", StringComparison.Ordinal));
    }

    private static void AssertNoForbiddenReferences(System.Reflection.Assembly assembly, params string[] forbiddenFragments)
    {
        var names = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
        foreach (var forbidden in forbiddenFragments)
            Assert.DoesNotContain(names, name => name.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
    }
}
