using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubastaYa.Domain.Entities;

namespace SubastaYa.Infrastructure.Data.Configurations;

public class SubastaConfiguration : IEntityTypeConfiguration<Subasta>
{
    public void Configure(EntityTypeBuilder<Subasta> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PrecioBase).HasPrecision(18, 2);
        builder.Property(x => x.IncrementoMinimo).HasPrecision(18, 2);

        builder.Property(x => x.Version)
               .IsRowVersion()
               .IsConcurrencyToken();

        // Índice para optimizar las consultas periódicas del Worker
        builder.HasIndex(x => new { x.Estado, x.FechaFin });
    }
}
