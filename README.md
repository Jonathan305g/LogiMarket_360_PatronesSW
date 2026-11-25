# LogiMarket 360

Aplicación empresarial de escritorio para ventas y logística. La solución separa Domain, Application, Infrastructure, WinForms y Tests. Los pagos, correos y SMS son simulados; no se conecta a servicios externos.

## Requisitos

- Windows 10/11 y Visual Studio 2022 con .NET 8 SDK y carga de trabajo de escritorio .NET.
- SQLite y Entity Framework Core 8 se restauran desde NuGet.
- Para comandos EF: `dotnet-ef` 8.x (`dotnet tool install --global dotnet-ef --version 8.0.11`).

## Compilar y ejecutar

Desde la raíz del repositorio:

```powershell
dotnet restore LogiMarket360.slnx
dotnet build LogiMarket360.slnx
dotnet run --project src/LogiMarket.WinForms/LogiMarket.WinForms.csproj
```

WinForms aplica las migraciones pendientes y ejecuta el seed idempotente al iniciar. La base predeterminada se guarda en `%LOCALAPPDATA%\LogiMarket360\logimarket.db`. La semilla crea P01–P05, KIT_OFICINA, KIT_TELETRABAJO, clientes de ejemplo y las cuatro cuentas de prueba.

Para trabajar con una base aislada, define `LOGIMARKET_DB_PATH` antes de iniciar. Por ejemplo:

```powershell
$env:LOGIMARKET_DB_PATH = Join-Path $env:TEMP 'logimarket-demo.db'
dotnet run --project src/LogiMarket.WinForms/LogiMarket.WinForms.csproj
```

Al cerrar la aplicación, se puede archivar o retirar esa base temporal sin tocar la base normal del usuario. El seed no restablece cantidades de inventario existentes.

## Migraciones

El arranque de WinForms aplica `Database.Migrate()`. Para aplicar manualmente:

```powershell
dotnet ef database update --project src/LogiMarket.Infrastructure/LogiMarket.Infrastructure.csproj --startup-project src/LogiMarket.Infrastructure/LogiMarket.Infrastructure.csproj
```

Para crear una migración desde un cambio de modelo:

```powershell
dotnet ef migrations add NombreCambio --project src/LogiMarket.Infrastructure/LogiMarket.Infrastructure.csproj --startup-project src/LogiMarket.Infrastructure/LogiMarket.Infrastructure.csproj --output-dir Migrations
```

## Cuentas locales de demostración

| Usuario | Contraseña | Rol |
| --- | --- | --- |
| `admin` | `Admin123!` | Admin |
| `ventas` | `Ventas123!` | Ventas |
| `bodega` | `Bodega123!` | Bodega |
| `consulta` | `Consulta123!` | Consulta |

Son cuentas ficticias para evaluación local. Las contraseñas se guardan con PBKDF2 y no deben reutilizarse en otros sistemas.

## Pruebas

```powershell
dotnet test LogiMarket360.slnx
```

Las pruebas de integración crean bases SQLite aisladas en la carpeta temporal; no usan ni borran la base de ejecución local. Los escenarios incluyen seed/migración, persistencia tras abrir otro contexto, restricciones, cobros, devoluciones, Outbox, concurrencia y patrones.

## Navegación y permisos

La ventana presenta acceso, panel general, catálogo y kits, clientes, carrito/borrador, pedidos e historial, cobro, despacho/entrega, devoluciones, inventario/movimientos, reportes/comprobantes y auditoría/demostración. La navegación se ajusta al rol y Application vuelve a comprobar permisos antes de cobros, despacho, devoluciones, cancelación y modificaciones de catálogo o clientes.

El modo de demostración permite elegir entre la pasarela legada adaptada y la pasarela directa, inducir un rechazo real en el colaborador simulado y hacer fallar una publicación Outbox para reintentarla.
