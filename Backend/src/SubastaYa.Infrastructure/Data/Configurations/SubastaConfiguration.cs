using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubastaYa.Domain.Entities;

namespace SubastaYa.Infrastructure.Data.Configurations;

public class SubastaConfiguration : IEntityTypeConfiguration<Subasta>
{
    public void Configure(EntityTypeBuilder<Subasta> builder)
    {
        builder.Property(x => x.PrecioBase).HasPrecision(18, 2);
        builder.Property(x => x.IncrementoMinimo).HasPrecision(18, 2);
        builder.Property(x => x.Version).IsRowVersion(); // Manejado automáticamente por Pomelo/MySQL

        // EL ÍNDICE PARA OPTIMIZAR EL WORKER
        builder.HasIndex(x => new { x.Estado, x.FechaFin });
    }
}
