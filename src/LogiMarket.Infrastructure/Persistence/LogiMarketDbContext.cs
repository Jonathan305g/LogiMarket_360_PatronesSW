using LogiMarket.Domain.Entities;
using LogiMarket.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence;

public class LogiMarketDbContext : DbContext
{
    public LogiMarketDbContext(DbContextOptions<LogiMarketDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<KitRegistro> Kits => Set<KitRegistro>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<MovimientoInventario> Movimientos => Set<MovimientoInventario>();
    public DbSet<Despacho> Despachos => Set<Despacho>();
    public DbSet<Devolucion> Devoluciones => Set<Devolucion>();
    public DbSet<EventoOutboxRegistro> EventosOutbox => Set<EventoOutboxRegistro>();
    public DbSet<AuditoriaRegistro> Auditorias => Set<AuditoriaRegistro>();
    public DbSet<AuditoriaSeguridadRegistro> AuditoriasSeguridad => Set<AuditoriaSeguridadRegistro>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LogiMarketDbContext).Assembly);
} 