using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubastaYa.Domain.Entities;

namespace SubastaYa.Infrastructure.Data.Configurations;

public class BilleteraConfiguration : IEntityTypeConfiguration<Billetera>
{
    public void Configure(EntityTypeBuilder<Billetera> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SaldoTotal).HasPrecision(18, 2);
        builder.Property(x => x.SaldoRetenido).HasPrecision(18, 2);

        // Asigna UUID() por defecto en MySQL si C# envía el campo vacío
        builder.Property(x => x.Version)
               .IsConcurrencyToken()
               .HasDefaultValueSql("(UUID())");
    }
}
