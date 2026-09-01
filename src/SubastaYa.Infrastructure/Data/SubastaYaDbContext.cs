using Microsoft.EntityFrameworkCore;
using SubastaYa.Domain.Entities;
using System.Reflection;

namespace SubastaYa.Infrastructure.Data;

public class SubastaYaDbContext : DbContext
{
    public SubastaYaDbContext(DbContextOptions<SubastaYaDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Subasta> Subastas { get; set; } = null!;
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Puja> Pujas { get; set; } = null!;
    public DbSet<Transaccion> Transacciones { get; set; } = null!;
    public DbSet<Auditoria> Auditorias { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas las configuraciones de IEntityTypeConfiguration de este ensamblado
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Aquí luego inyectaremos el Seed Data
    }
}
