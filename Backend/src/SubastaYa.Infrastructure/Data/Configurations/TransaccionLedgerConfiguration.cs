using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubastaYa.Domain.Entities;

namespace SubastaYa.Infrastructure.Data.Configurations;

public class TransaccionLedgerConfiguration : IEntityTypeConfiguration<TransaccionLedger>
{
    public void Configure(EntityTypeBuilder<TransaccionLedger> builder)
    {
        builder.Property(x => x.Monto).HasPrecision(18, 2);

        builder.Property(x => x.Tipo)
               .HasConversion<string>()
               .HasMaxLength(20);
    }
}
