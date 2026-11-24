using LogiMarket.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMarket.Infrastructure.Persistence.Configurations;

public class EventoOutboxRegistroConfiguration : IEntityTypeConfiguration<EventoOutboxRegistro>
{
    public void Configure(EntityTypeBuilder<EventoOutboxRegistro> builder)
    {
        builder.ToTable("EventosOutbox");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Tipo).IsRequired().HasMaxLength(60);
        builder.Property(e => e.DatosJson).IsRequired();
        builder.HasIndex(e => e.ProcesadoUtc); // para hallar rápido los pendientes (null)
    }
}

public class AuditoriaRegistroConfiguration : IEntityTypeConfiguration<AuditoriaRegistro>
{
    public void Configure(EntityTypeBuilder<AuditoriaRegistro> builder)
    {
        builder.ToTable("Auditorias");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.TipoEvento).IsRequired().HasMaxLength(60);
        builder.Property(a => a.Detalle).IsRequired();
    }
}

public class AuditoriaSeguridadRegistroConfiguration : IEntityTypeConfiguration<AuditoriaSeguridadRegistro>
{
    public void Configure(EntityTypeBuilder<AuditoriaSeguridadRegistro> builder)
    {
        builder.ToTable("AuditoriasSeguridad"); 
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Usuario).IsRequired().HasMaxLength(60);
        builder.Property(a => a.Rol).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Operacion).IsRequired().HasMaxLength(60);
    }
}