using System.Reflection;
using LogiMarket.Infrastructure.Persistence;
using LogiMarket.Infrastructure.Persistence.Seed;
using LogiMarket.WinForms.Controles;
using LogiMarket.WinForms.Formularios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.Tests.Architecture;

public sealed class CompositionRootTests
{
    [Fact]
    public async Task Arranque_MigraYSiembraYResuelveLasDocePantallas()
    {
        var rutaBd = Path.Combine(Path.GetTempPath(), $"logimarket_arranque_{Guid.NewGuid():N}.db");
        var variableAnterior = Environment.GetEnvironmentVariable("LOGIMARKET_DB_PATH");
        Environment.SetEnvironmentVariable("LOGIMARKET_DB_PATH", rutaBd);

        try
        {
            var tipoPrograma = typeof(FormularioPrincipal).Assembly.GetType("LogiMarket.WinForms.Program")!;
            var configurar = tipoPrograma.GetMethod("ConfigurarServicios", BindingFlags.NonPublic | BindingFlags.Static)!;
            using var servicios = (ServiceProvider)configurar.Invoke(null, null)!;
            using var alcance = servicios.CreateScope();
            var proveedor = alcance.ServiceProvider;

            var db = proveedor.GetRequiredService<LogiMarketDbContext>();
            await db.Database.MigrateAsync();
            await DatosSemilla.SembrarAsync(db);

            var pantallas = new[]
            {
                typeof(PanelAcceso), typeof(PanelGeneral), typeof(PanelCatalogo), typeof(PanelClientes),
                typeof(PanelCarrito), typeof(PanelPedidos), typeof(PanelCobro), typeof(PanelDespacho),
                typeof(PanelDevoluciones), typeof(PanelInventario), typeof(PanelReportes), typeof(PanelAuditoria)
            };

            foreach (var pantalla in pantallas)
                Assert.NotNull(proveedor.GetRequiredService(pantalla));

            Assert.Equal(5, await db.Productos.CountAsync());
            Assert.Equal(2, await db.Kits.CountAsync());
            Assert.Equal(4, await db.Usuarios.CountAsync());
            Assert.NotNull(proveedor.GetRequiredService<FormularioPrincipal>());
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOGIMARKET_DB_PATH", variableAnterior);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(rutaBd)) File.Delete(rutaBd);
        }
    }
}
