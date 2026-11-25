using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using LogiMarket.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Repositories;

public sealed class AdministracionCatalogoRepositorioEf(LogiMarketDbContext db) : IAdministracionCatalogoRepository
{
    public Task<Producto?> ObtenerProductoParaEdicionAsync(string sku, CancellationToken ct = default) =>
        db.Productos.FirstOrDefaultAsync(p => p.Sku == sku, ct);

    public async Task<bool> SkuExisteAsync(string sku, CancellationToken ct = default) =>
        await db.Productos.AnyAsync(p => p.Sku == sku, ct)
        || await db.Kits.AnyAsync(k => k.Sku == sku, ct);

    public void AgregarProducto(Producto producto) => db.Productos.Add(producto);

    public async Task<bool> EliminarProductoAsync(string sku, CancellationToken ct = default)
    {
        var producto = await db.Productos.FirstOrDefaultAsync(p => p.Sku == sku, ct);
        if (producto is null) return false;
        if (await db.Set<KitComponenteRegistro>().AnyAsync(c => !c.EsKit && c.ComponenteSku == sku, ct)
            || await db.Movimientos.AnyAsync(m => m.Sku == sku, ct)
            || await db.Pedidos.SelectMany(p => p.Detalles).AnyAsync(d =>
                d.Sku == sku || d.Componentes.Any(c => c.Sku == sku), ct))
            return false;

        db.Productos.Remove(producto);
        return true;
    }

    public async Task GuardarKitAsync(
        string sku, string nombre, IReadOnlyList<ComponenteKitSolicitud> componentes, CancellationToken ct = default)
    {
        var kit = await db.Kits.Include(k => k.Componentes).FirstOrDefaultAsync(k => k.Sku == sku, ct);
        if (kit is null)
        {
            kit = new KitRegistro { Sku = sku, Nombre = nombre };
            db.Kits.Add(kit);
        }
        else
        {
            kit.Nombre = nombre;
            kit.Componentes.Clear();
        }

        foreach (var componente in componentes)
            kit.Componentes.Add(new KitComponenteRegistro
            {
                KitSku = sku,
                ComponenteSku = componente.Sku,
                EsKit = componente.EsKit,
                ProductoSku = componente.EsKit ? null : componente.Sku,
                KitReferenciaSku = componente.EsKit ? componente.Sku : null,
                Cantidad = componente.Cantidad
            });
    }

    public async Task<bool> EliminarKitAsync(string sku, CancellationToken ct = default)
    {
        var kit = await db.Kits.FirstOrDefaultAsync(k => k.Sku == sku, ct);
        if (kit is null) return false;
        if (await db.Set<KitComponenteRegistro>().AnyAsync(c => c.EsKit && c.ComponenteSku == sku, ct)
            || await db.Pedidos.SelectMany(p => p.Detalles).AnyAsync(d => d.Sku == sku, ct))
            return false;

        db.Kits.Remove(kit);
        return true;
    }
}
