using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Infrastructure.Data
{
    public class SubastaYaDbContextFactory
    {
        public SubastaYaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SubastaYaDbContext>();

            // Cadena de conexión directa para que las CLI tools de EF Core generen la migración sin fallar
            var connectionString = "Server=localhost;Port=3306;Database=SubastaYaDb;Uid=subastaya_user;Pwd=subastaya_password;";

            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new SubastaYaDbContext(optionsBuilder.Options);
        }
    }
}
