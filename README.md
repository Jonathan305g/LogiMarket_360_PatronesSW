# LogiMarket 360 — Control Comercial y Logístico

Aplicación empresarial de escritorio desarrollada en C# y Windows Forms (.NET 8) bajo arquitectura limpia (Clean Architecture), principios SOLID y los 23 patrones de diseño GoF. El sistema coordina catálogo y kits compuestos, clientes, pedidos con ciclo de vida completo, cobros simulados, despacho, devoluciones, inventario trazable, reportes y auditoría.

---

## 1. Requisitos del Sistema

- **Sistema Operativo:** Windows 10 / Windows 11 (64-bit).
- **Entorno de Ejecución:** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (versión 8.0.100 o superior).
- **Entorno de Desarrollo sugerido:** Visual Studio 2022 (con la carga de trabajo *Desarrollo de escritorio de .NET*) o Visual Studio Code / JetBrains Rider.
- **Herramienta opcional para EF Core:** `dotnet-ef` versión 8.x.

---

## 2. Instalación de .NET 8 y Paquetes

1. Descargar e instalar el **.NET 8 SDK** oficial desde el portal de Microsoft si no se encuentra instalado. Verificar en la terminal:
   ```powershell
   dotnet --version
   ```
2. (Opcional) Instalar la herramienta global de Entity Framework Core para gestión de migraciones:
   ```powershell
   dotnet tool install --global dotnet-ef --version 8.0.11
   ```

---

## 3. Restauración de Dependencias

Desde la raíz del repositorio, restaurar todos los paquetes NuGet de la solución:

```powershell
dotnet restore LogiMarket360.slnx
```

Los paquetes clave que se descargan automáticamente incluyen:
- `Microsoft.EntityFrameworkCore.Sqlite` (8.0.11)
- `Microsoft.EntityFrameworkCore.Design` (8.0.11)
- `Microsoft.Extensions.DependencyInjection` (8.0.1)
- `xunit` (2.9.3) y `Microsoft.NET.Test.Sdk`

---

## 4. Compilación de la Solución

Compilar los 5 proyectos del sistema (`Domain`, `Application`, `Infrastructure`, `WinForms` y `Tests`):

```powershell
dotnet build LogiMarket360.slnx
```

La compilación debe finalizar con **0 advertencias** y **0 errores**.

---

## 5. Migración y Datos Semilla (Seed)

La aplicación ejecuta automáticamente `Database.Migrate()` y la siembra idempotente de datos (`DatosSemilla.SembrarAsync`) durante el inicio de WinForms.

- **Ubicación predeterminada de la base SQLite:**
  ```text
  %LOCALAPPDATA%\LogiMarket360\logimarket.db
  ```
- **Datos iniciales sembrados:**
  - 5 Productos estándar (`P01` a `P05`) con precios, stock y peso.
  - 2 Kits anidados (`KIT_OFICINA`, `KIT_TELETRABAJO`) utilizando el patrón Composite.
  - Clientes de ejemplo con categorías Minorista, VIP y Mayorista.
  - 4 Cuentas de usuario de prueba con roles asignados.
  - Stock inicial e historial de movimientos.

Si se requiere aplicar las migraciones manualmente mediante CLI:
```powershell
dotnet ef database update --project src/LogiMarket.Infrastructure/LogiMarket.Infrastructure.csproj --startup-project src/LogiMarket.Infrastructure/LogiMarket.Infrastructure.csproj
```

---

## 6. Ejecución de la Aplicación WinForms

Iniciar la aplicación desde la terminal en la raíz del repositorio:

```powershell
dotnet run --project src/LogiMarket.WinForms/LogiMarket.WinForms.csproj
```

La interfaz principal cargará automáticamente la pantalla de **Acceso (Login)**.

---

## 7. Cuentas Locales de Demostración

El sistema incluye 4 cuentas precargadas en el seed para evaluar el control de acceso y navegación por roles (Proxy / Mediator):

| Usuario | Contraseña | Rol asignado | Permisos y Paneles Permitidos |
| :--- | :--- | :--- | :--- |
| `admin` | `Admin123!` | **Admin** | Acceso total: catálogo, kits, clientes, pedidos, cobro, despacho, devoluciones, inventario, reportes y auditoría. |
| `ventas` | `Ventas123!` | **Ventas** | Clientes, carrito/borrador, cobro, pedidos, devoluciones y reportes. |
| `bodega` | `Bodega123!` | **Bodega** | Catálogo (lectura), inventario/ajustes y panel de despacho/entrega. |
| `consulta` | `Consulta123!` | **Consulta** | Paneles de solo lectura: catálogo, pedidos, inventario y reportes. |

> **Nota:** Las contraseñas están almacenadas mediante hash seguro **PBKDF2** con sal criptográfica única y no en texto plano.

---

## 8. Comando de Pruebas Automatizadas

Ejecutar la suite completa de pruebas unitarias, de integración y de patrones de software:

```powershell
dotnet test LogiMarket360.slnx
```

- **Total de pruebas:** 221 pruebas automatizadas (100% aprobadas, 0 fallidas, 0 omitidas).
- **Aislamiento:** Las pruebas de infraestructura y persistencia generan bases SQLite temporales en memoria o en la carpeta temporal, garantizando que la base de datos local del usuario no se altere.
- **Cobertura:** Cubre los 23 patrones GoF, reglas de negocio, cálculo de descuentos e impuestos, Outbox transaccional, invariantes de dominio y concurrencia.

---

## 9. Forma Segura de Restablecer Datos de Prueba

Para restablecer el sistema a su estado inicial limpio o realizar pruebas aisladas sin afectar datos previos, se disponen de dos métodos seguros:

### Opción A: Restablecimiento completo de la base local
Cerrar la aplicación y eliminar el archivo de base de datos local ejecutando en PowerShell:
```powershell
Remove-Item "$env:LOCALAPPDATA\LogiMarket360\logimarket.db*" -Force -ErrorAction SilentlyContinue
```
Al volver a iniciar la aplicación (`dotnet run ...`), se recreará la base de datos SQLite desde cero y se aplicará la semilla de fábrica automáticamente.

### Opción B: Uso de base de datos temporal aislada (Recomendado para demos)
Definir la variable de entorno `LOGIMARKET_DB_PATH` antes de ejecutar la aplicación:
```powershell
$env:LOGIMARKET_DB_PATH = Join-Path $env:TEMP "logimarket-demo-$([System.Guid]::NewGuid()).db"
dotnet run --project src/LogiMarket.WinForms/LogiMarket.WinForms.csproj
```
Al finalizar la demostración, la base temporal se puede descartar sin comprometer la base habitual.
