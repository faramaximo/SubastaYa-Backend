using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubastaYa.Domain.Entities;

namespace SubastaYa.Infrastructure.Data.Configurations;

public class AuditoriaLogConfiguration : IEntityTypeConfiguration<AuditoriaLog>
{
    public void Configure(EntityTypeBuilder<AuditoriaLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Entidad)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Accion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.DetalleJson)
            .HasColumnType("json");

        // Bloquear actualizaciones de todos los campos a nivel EF Core
        foreach (var property in builder.Metadata.GetProperties())
        {
            property.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
        }
    }
}
