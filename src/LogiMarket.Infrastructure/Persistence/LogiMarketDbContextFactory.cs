using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LogiMarket.Infrastructure.Persistence;

// Permite ejecutar "dotnet ef" sobre ESTE proyecto solo, sin depender de WinForms
// (el composition root con DI todavía no existe).
public class LogiMarketDbContextFactory : IDesignTimeDbContextFactory<LogiMarketDbContext>
{
    public LogiMarketDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<LogiMarketDbContext>()
            .UseSqlite("Data Source=logimarket_diseno.db")
            .Options;

        return new LogiMarketDbContext(opciones);
    }
}