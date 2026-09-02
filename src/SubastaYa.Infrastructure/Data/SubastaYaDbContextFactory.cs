using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SubastaYa.Infrastructure.Data
{
    // Es clave que implemente IDesignTimeDbContextFactory para que la consola no falle
    public class SubastaYaDbContextFactory : IDesignTimeDbContextFactory<SubastaYaDbContext>
    {
        public SubastaYaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SubastaYaDbContext>();

            // ¡Tus credenciales locales!
            var connectionString = "Server=localhost;Port=3306;Database=SubastaYaDb;Uid=root;Pwd=1234;";

            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new SubastaYaDbContext(optionsBuilder.Options);
        }
    }
}
