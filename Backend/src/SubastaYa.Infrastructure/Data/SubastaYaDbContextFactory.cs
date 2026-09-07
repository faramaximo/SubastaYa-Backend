using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;
using System.Text.Json;
using System;

namespace SubastaYa.Infrastructure.Data
{
    // Implementación de IDesignTimeDbContextFactory para herramientas de EF Core (migrations)
    public class SubastaYaDbContextFactory : IDesignTimeDbContextFactory<SubastaYaDbContext>
    {
        public SubastaYaDbContext CreateDbContext(string[] args)
        {
            // Intentar leer la cadena de conexión desde variables de entorno (ConnectionStrings__DefaultConnection)
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                                   ?? Environment.GetEnvironmentVariable("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback: leer appsettings.Development.json del proyecto WebApi
                var configPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SubastaYa.WebApi", "appsettings.Development.json");
                if (!File.Exists(configPath))
                {
                    configPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SubastaYa.WebApi", "appsettings.json");
                }

                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) && cs.TryGetProperty("DefaultConnection", out var def))
                        {
                            connectionString = def.GetString();
                        }
                    }
                    catch
                    {
                        // ignorar y dejar connectionString null si falla el parseo
                    }
                }
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection' en variables de entorno ni en appsettings del proyecto WebApi.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<SubastaYaDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new SubastaYaDbContext(optionsBuilder.Options);
        }
    }
}
