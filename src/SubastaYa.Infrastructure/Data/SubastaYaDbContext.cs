using Microsoft.EntityFrameworkCore;
using SubastaYa.Domain.Entities;
using System.Reflection;

namespace SubastaYa.Infrastructure.Data;

public class SubastaYaDbContext : DbContext
{
    public SubastaYaDbContext(DbContextOptions<SubastaYaDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Billetera> Billeteras => Set<Billetera>();
    public DbSet<Subasta> Subastas => Set<Subasta>();
    public DbSet<Puja> Pujas => Set<Puja>();
    public DbSet<TransaccionLedger> TransaccionesLedger => Set<TransaccionLedger>();
    public DbSet<AuditoriaLog> AuditoriasLog => Set<AuditoriaLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mapeo preciso de decimales
        modelBuilder.Entity<Billetera>(b =>
        {
            b.Property(x => x.SaldoTotal).HasPrecision(18, 2);
            b.Property(x => x.SaldoRetenido).HasPrecision(18, 2);
            b.Property(x => x.Version).IsRowVersion(); // Manejado automáticamente por Pomelo/MySQL
        });

        modelBuilder.Entity<Subasta>(s =>
        {
            s.Property(x => x.PrecioBase).HasPrecision(18, 2);
            s.Property(x => x.IncrementoMinimo).HasPrecision(18, 2);
            s.Property(x => x.Version).IsRowVersion(); // Manejado automáticamente por Pomelo/MySQL
        });

        modelBuilder.Entity<Puja>(p =>
        {
            p.Property(x => x.Monto).HasPrecision(18, 2);
        });

        modelBuilder.Entity<TransaccionLedger>(t =>
        {
            t.Property(x => x.Monto).HasPrecision(18, 2);
        });
    }
}

