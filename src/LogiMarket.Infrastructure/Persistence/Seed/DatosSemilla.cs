using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.ValueObjects;
using LogiMarket.Infrastructure.Persistence.Models;
using LogiMarket.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Seed;

public static class DatosSemilla
{
    public static async Task SembrarAsync(LogiMarketDbContext db, CancellationToken ct = default)
    {
        await SembrarProductosAsync(db, ct);
        await SembrarKitsAsync(db, ct);
        await SembrarUsuariosAsync(db, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SembrarProductosAsync(LogiMarketDbContext db, CancellationToken ct)
    {
        var oficiales = new (string Sku, string Nombre, decimal Precio, decimal Peso, int Stock)[]
        {
            ("P01", "Mouse USB", 20.00m, 0.20m, 8),
            ("P02", "Teclado", 30.00m, 0.70m, 5),
            ("P03", "Memoria USB", 10.00m, 0.10m, 20),
            ("P04", "Webcam", 40.00m, 0.35m, 4),
            ("P05", "Alfombrilla", 5.00m, 0.10m, 30),
        };

        foreach (var o in oficiales)
        {
            // Idempotente: si el SKU ya existe, NO se vuelve a insertar ni se pisa el stock actual.
            if (!await db.Productos.AnyAsync(p => p.Sku == o.Sku, ct))
                db.Productos.Add(new Producto(o.Sku, o.Nombre, new Dinero(o.Precio), o.Peso, o.Stock));
        }
    }

    private static async Task SembrarKitsAsync(LogiMarketDbContext db, CancellationToken ct)
    {
        if (!await db.Kits.AnyAsync(k => k.Sku == "KIT_OFICINA", ct))
        {
            db.Kits.Add(new KitRegistro
            {
                Sku = "KIT_OFICINA",
                Nombre = "Kit Oficina",
                Componentes =
                {
                    new KitComponenteRegistro { ComponenteSku = "P01", EsKit = false, Cantidad = 1 },
                    new KitComponenteRegistro { ComponenteSku = "P02", EsKit = false, Cantidad = 1 },
                }
            });
        }

        if (!await db.Kits.AnyAsync(k => k.Sku == "KIT_TELETRABAJO", ct))
        {
            db.Kits.Add(new KitRegistro
            {
                Sku = "KIT_TELETRABAJO",
                Nombre = "Kit Teletrabajo",
                Componentes =
                {
                    new KitComponenteRegistro { ComponenteSku = "KIT_OFICINA", EsKit = true, Cantidad = 1 },
                    new KitComponenteRegistro { ComponenteSku = "P03", EsKit = false, Cantidad = 1 },
                }
            });
        }
    }

    private static async Task SembrarUsuariosAsync(LogiMarketDbContext db, CancellationToken ct)
    {
        var hasher = new HasherContrasenaPbkdf2();
        var cuentas = new (string Usuario, string Contrasena, RolUsuario Rol)[]
        {
            ("admin", "Admin123!", RolUsuario.Admin),
            ("ventas", "Ventas123!", RolUsuario.Ventas),
            ("bodega", "Bodega123!", RolUsuario.Bodega),
            ("consulta", "Consulta123!", RolUsuario.Consulta),
        };

        foreach (var (nombre, contrasena, rol) in cuentas)
        {
            var existente = await db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombre, ct);
            if (existente is null)
                db.Usuarios.Add(new Usuario(nombre, hasher.Hashear(contrasena), rol));
            else if (existente.HashContrasena == HashLegadoSinSal(nombre))
                existente.ActualizarHashContrasena(hasher.Hashear(contrasena));
        }
    }

    private static string HashLegadoSinSal(string valor) =>
        Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(valor)));
}
