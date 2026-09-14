using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubastaYa.Domain.Entities;

namespace SubastaYa.Infrastructure.Data.Configurations;

public class AuditoriaLogConfiguration : IEntityTypeConfiguration<AuditoriaLog>
{
    public void Configure(EntityTypeBuilder<AuditoriaLog> builder)
    {
    }
}
