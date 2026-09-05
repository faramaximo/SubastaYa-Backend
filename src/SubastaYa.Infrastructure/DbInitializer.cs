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
            if (await context.Usuarios.AnyAsync())
            {
                // Traemos las subastas ordenadas por ID
                var subastasExistentes = await context.Subastas.OrderBy(s => s.Id).ToListAsync();

                if (subastasExistentes.Count >= 5)
                {
                    // 1. Activa estándar: Cierra en 30 min
                    subastasExistentes[0].FechaFin = DateTime.UtcNow.AddMinutes(30);
                    subastasExistentes[0].Estado = EstadoSubasta.Activa;

                    // 2. Activa crítica: Cierra en menos de 2 min (1 min 50 seg)
                    subastasExistentes[1].FechaFin = DateTime.UtcNow.AddSeconds(110);
                    subastasExistentes[1].Estado = EstadoSubasta.Activa;

                    // 3. Próxima: Inicio programado a +24 hs
                    subastasExistentes[2].FechaInicio = DateTime.UtcNow.AddHours(24);
                    subastasExistentes[2].FechaFin = DateTime.UtcNow.AddHours(48);
                    subastasExistentes[2].Estado = EstadoSubasta.Programada;

                    // 4. Vencida con ganador: Fecha fin en el pasado
                    subastasExistentes[3].FechaFin = DateTime.UtcNow.AddMinutes(-10);
                    subastasExistentes[3].Estado = EstadoSubasta.Activa;

                    // 5. Vencida desierta: Fecha fin en el pasado lejano
                    subastasExistentes[4].FechaFin = DateTime.UtcNow.AddHours(-2);
                    subastasExistentes[4].Estado = EstadoSubasta.Activa;

                    // Guardamos los cambios forzadamente en la base de datos
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ TIEMPOS REINICIADOS A LOS CASOS DE PRUEBA DEL TP");
                }
                return; // Cortamos acá para no duplicar toda la base
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
            var subastas = new List<Subasta>
            {
                // Caso 1: Activa estándar (Cierra en 30 min)
                new Subasta { VendedorId = usuarios[0].Id, CategoriaId = categorias[0].Id, Titulo = "Casco VR Edición Coleccionista", Descripcion = "Casco tope de gama.", UrlImagen = categorias[0].ImagenUrl, PrecioBase = 10000, IncrementoMinimo = 2000, FechaInicio = DateTime.UtcNow.AddMinutes(-30), FechaFin = DateTime.UtcNow.AddMinutes(30), Estado = EstadoSubasta.Activa },
                
                // Caso 2: Activa crítica (Cierra en 1 min 50 seg - Anti-Sniping)
                new Subasta { VendedorId = usuarios[0].Id, CategoriaId = categorias[1].Id, Titulo = "Consola Retro Edición Limitada", Descripcion = "Consola clásica.", UrlImagen = categorias[1].ImagenUrl, PrecioBase = 50000, IncrementoMinimo = 5000, FechaInicio = DateTime.UtcNow.AddMinutes(-58), FechaFin = DateTime.UtcNow.AddSeconds(110), Estado = EstadoSubasta.Activa },
                
                // Caso 3: Próxima (+24hs)
                new Subasta { VendedorId = usuarios[0].Id, CategoriaId = categorias[3].Id, Titulo = "Llave NFT Prototipo Deportivo", Descripcion = "Acceso exclusivo.", UrlImagen = categorias[3].ImagenUrl, PrecioBase = 100000, IncrementoMinimo = 10000, FechaInicio = DateTime.UtcNow.AddHours(24), FechaFin = DateTime.UtcNow.AddHours(48), Estado = EstadoSubasta.Programada },
                
                // Caso 4: Vencida con ganador
                new Subasta { VendedorId = usuarios[0].Id, CategoriaId = categorias[2].Id, Titulo = "Render Abstracto Ciberpunk #3", Descripcion = "Arte digital.", UrlImagen = categorias[2].ImagenUrl, PrecioBase = 15000, IncrementoMinimo = 1000, FechaInicio = DateTime.UtcNow.AddHours(-3), FechaFin = DateTime.UtcNow.AddMinutes(-10), Estado = EstadoSubasta.Activa },
                
                // Caso 5: Vencida desierta (Sin pujas)
                new Subasta { VendedorId = usuarios[0].Id, CategoriaId = categorias[0].Id, Titulo = "Cable HDMI Dañado", Descripcion = "Objeto sin uso.", UrlImagen = categorias[0].ImagenUrl, PrecioBase = 1000, IncrementoMinimo = 100, FechaInicio = DateTime.UtcNow.AddHours(-5), FechaFin = DateTime.UtcNow.AddHours(-2), Estado = EstadoSubasta.Activa }
            };
            context.Subastas.AddRange(subastas);
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