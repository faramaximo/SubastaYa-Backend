using Microsoft.EntityFrameworkCore;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.Infrastructure.Seed
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(SubastaYaDbContext context)
        {
            await context.Database.MigrateAsync();

            // ====================================================================
            // 💡 REINICIO DE RELOJES PARA LA DEFENSA DEL TP (Módulo 1)
            // ====================================================================
            // ====================================================================
            // 💡 REINICIO DE RELOJES PARA LA DEFENSA DEL TP (Módulo 1)
            // ====================================================================
            if (await context.Usuarios.AnyAsync())
            {
                var subastasExistentes = await context.Subastas.OrderBy(s => s.Id).ToListAsync();

                if (subastasExistentes.Count >= 5)
                {
                    // Usamos context.Entry() para saltarnos el "private set" y modificar la base de datos directamente

                    // 1. Activa estándar: Cierra en 30 min
                    context.Entry(subastasExistentes[0]).Property(s => s.FechaFin).CurrentValue = DateTime.UtcNow.AddMinutes(30);
                    context.Entry(subastasExistentes[0]).Property(s => s.Estado).CurrentValue = EstadoSubasta.Activa;

                    // 2. Activa crítica: Cierra en menos de 2 min (1 min 50 seg)
                    context.Entry(subastasExistentes[1]).Property(s => s.FechaFin).CurrentValue = DateTime.UtcNow.AddSeconds(110);
                    context.Entry(subastasExistentes[1]).Property(s => s.Estado).CurrentValue = EstadoSubasta.Activa;

                    // 3. Próxima: Inicio programado a +24 hs
                    context.Entry(subastasExistentes[2]).Property(s => s.FechaInicio).CurrentValue = DateTime.UtcNow.AddHours(24);
                    context.Entry(subastasExistentes[2]).Property(s => s.FechaFin).CurrentValue = DateTime.UtcNow.AddHours(48);
                    context.Entry(subastasExistentes[2]).Property(s => s.Estado).CurrentValue = EstadoSubasta.Programada;

                    // 4. Vencida con ganador: Fecha fin en el pasado
                    context.Entry(subastasExistentes[3]).Property(s => s.FechaFin).CurrentValue = DateTime.UtcNow.AddMinutes(-10);
                    context.Entry(subastasExistentes[3]).Property(s => s.Estado).CurrentValue = EstadoSubasta.Activa;

                    // 5. Vencida desierta: Fecha fin en el pasado lejano
                    context.Entry(subastasExistentes[4]).Property(s => s.FechaFin).CurrentValue = DateTime.UtcNow.AddHours(-2);
                    context.Entry(subastasExistentes[4]).Property(s => s.Estado).CurrentValue = EstadoSubasta.Activa;

                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ TIEMPOS REINICIADOS A LOS CASOS DE PRUEBA DEL TP");
                }
                return;
            }

            // ====================================================================
            // 1. CATEGORÍAS OBLIGATORIAS
            // ====================================================================
            var categorias = new List<Categoria>
            {
                new Categoria { Nombre = "Tecnología", ImagenUrl = "https://images.unsplash.com/photo-1550745165-9bc0b252726f" },
                new Categoria { Nombre = "Coleccionables", ImagenUrl = "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f" },
                new Categoria { Nombre = "Arte", ImagenUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe" },
                new Categoria { Nombre = "Vehículos", ImagenUrl = "https://images.unsplash.com/photo-1552519507-da3b142c6e3d" }
            };
            context.Categorias.AddRange(categorias);
            await context.SaveChangesAsync();


            // ====================================================================
            // 2. USUARIOS Y BILLETERAS OBLIGATORIAS
            // ====================================================================

            // Encriptamos una contraseña genérica ("123456") para todos
            string passwordHasheada = BCrypt.Net.BCrypt.HashPassword("123456");

            var usuarios = new List<Usuario>
            {
                new Usuario { Nombre = "Vendedor Test", Email = "vendedor@test.com", PasswordHash = passwordHasheada, FechaRegistro = DateTime.UtcNow },
                new Usuario { Nombre = "Comprador Líder", Email = "comprador1@test.com", PasswordHash = passwordHasheada, FechaRegistro = DateTime.UtcNow },
                new Usuario { Nombre = "Comprador Dos", Email = "comprador2@test.com", PasswordHash = passwordHasheada, FechaRegistro = DateTime.UtcNow },
                new Usuario { Nombre = "Usuario Sin Fondos", Email = "sinfondos@test.com", PasswordHash = passwordHasheada, FechaRegistro = DateTime.UtcNow }
            };

            context.Usuarios.AddRange(usuarios);
            await context.SaveChangesAsync(); // Guardamos para que se generen los IDs

            var billeteras = new List<Billetera>
            {
                new Billetera(usuarios[0].Id, 0, 0),
                new Billetera(usuarios[1].Id, 150000, 45000),
                new Billetera(usuarios[2].Id, 200000, 0),
                new Billetera(usuarios[3].Id, 500, 0)
            };
            context.Billeteras.AddRange(billeteras);
            await context.SaveChangesAsync();


            // ====================================================================
            // 3. LAS 5 SUBASTAS DEL TP
            // ====================================================================
            // Creamos las 5 usando el constructor (nacerán como 'Activas')
            // ====================================================================
            // 3. LAS 5 SUBASTAS DEL TP
            // ====================================================================
            // Creamos las 5 usando el constructor (nacerán como 'Activas')
            var subastas = new List<Subasta>
            {
                // 0: Activa estándar (Vehículo)
                new Subasta(usuarios[0].Id, categorias[3].Id, "Ford Fiesta", "Buen estado", "https://images.unsplash.com/photo-1552519507-da3b142c6e3d", 15000m, 500m, DateTime.UtcNow.AddMinutes(30)),
                
                // 1: Activa crítica (Tecnología)
                new Subasta(usuarios[0].Id, categorias[0].Id, "Laptop Gamer", "Casi nueva", "https://images.unsplash.com/photo-1550745165-9bc0b252726f", 80000m, 1000m, DateTime.UtcNow.AddSeconds(110)),
                
                // 2: Próxima (Coleccionables)
                new Subasta(usuarios[0].Id, categorias[1].Id, "Reloj Antiguo", "Siglo XIX", "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f", 50000m, 2000m, DateTime.UtcNow.AddHours(48)),
                
                // 3: Vencida con ganador (Arte)
                new Subasta(usuarios[0].Id, categorias[2].Id, "Cuadro Original", "Pintura al óleo", "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe", 30000m, 1500m, DateTime.UtcNow.AddDays(1)),
                
                // 4: Vencida desierta (Tecnología)
                new Subasta(usuarios[0].Id, categorias[0].Id, "Auriculares", "In-ear", "https://images.unsplash.com/photo-1550745165-9bc0b252726f", 5000m, 100m, DateTime.UtcNow.AddDays(1))
            };

            context.Subastas.AddRange(subastas);
            await context.SaveChangesAsync();

            // Ajustamos los estados específicos saltándonos el "private set" para los casos 2, 3 y 4
            context.Entry(subastas[2]).Property(s => s.FechaInicio).CurrentValue = DateTime.UtcNow.AddHours(24);
            context.Entry(subastas[2]).Property(s => s.Estado).CurrentValue = EstadoSubasta.Programada;

            context.Entry(subastas[3]).Property(s => s.FechaFin).CurrentValue = DateTime.UtcNow.AddMinutes(-10);
            context.Entry(subastas[4]).Property(s => s.FechaFin).CurrentValue = DateTime.UtcNow.AddHours(-2);

            await context.SaveChangesAsync();


            // ====================================================================
            // 4. PUJAS OBLIGATORIAS (Punto 3.3 del TP)
            // ====================================================================
            var pujas = new List<Puja>
            {
                // Puja 1: Comprador 2 oferta $20.000 en el Casco VR
                new Puja { SubastaId = subastas[0].Id, CompradorId = usuarios[2].Id, Monto = 20000, FechaPuja = DateTime.UtcNow.AddMinutes(-20) },
                
                // Puja 2: Comprador 1 (Líder) supera con $45.000 en el Casco VR
                new Puja { SubastaId = subastas[0].Id, CompradorId = usuarios[1].Id, Monto = 45000, FechaPuja = DateTime.UtcNow.AddMinutes(-10) },
                
                // Puja 3: Para el caso 4 (Vencida con ganador)
                new Puja { SubastaId = subastas[3].Id, CompradorId = usuarios[1].Id, Monto = 18000, FechaPuja = DateTime.UtcNow.AddHours(-1) }
            };
            context.Pujas.AddRange(pujas);
            await context.SaveChangesAsync();
        }
    }
}