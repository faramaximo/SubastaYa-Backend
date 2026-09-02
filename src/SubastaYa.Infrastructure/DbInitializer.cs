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
            try
            {
                // 1. Categorías obligatorias
                if (!await context.Categorias.AnyAsync())
                {
                    var categorias = new List<Categoria>
                    {
                        new Categoria { Nombre = "Tecnología", ImagenUrl = "https://images.unsplash.com/photo-1550745165-9bc0b252726f" },
                        new Categoria { Nombre = "Coleccionables", ImagenUrl = "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f" },
                        new Categoria { Nombre = "Arte", ImagenUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe" },
                        new Categoria { Nombre = "Vehículos", ImagenUrl = "https://images.unsplash.com/photo-1552519507-da3b142c6e3d" }
                    };
                    context.Categorias.AddRange(categorias);
                    await context.SaveChangesAsync();
                }

                // 2. Usuarios obligatorios
                if (!await context.Usuarios.AnyAsync())
                {
                    var usuarios = new List<Usuario>
                    {
                        new Usuario { Email = "vendedor@test.com", Nombre = "Vendedor Test", PasswordHash = "hash123", FechaRegistro = DateTime.UtcNow },
                        new Usuario { Email = "comprador1@test.com", Nombre = "Comprador Líder", PasswordHash = "hash123", FechaRegistro = DateTime.UtcNow },
                        new Usuario { Email = "comprador2@test.com", Nombre = "Comprador Activo", PasswordHash = "hash123", FechaRegistro = DateTime.UtcNow },
                        new Usuario { Email = "sinfondos@test.com", Nombre = "Usuario Sin Saldo", PasswordHash = "hash123", FechaRegistro = DateTime.UtcNow }
                    };
                    context.Usuarios.AddRange(usuarios);
                    await context.SaveChangesAsync();
                }

                // 3. Asegurar que cada usuario tenga una billetera
                var usuariosParaBilletera = await context.Usuarios.ToListAsync();
                if (usuariosParaBilletera.Any())
                {
                    var usuariosConBilletera = await context.Billeteras
                        .Select(b => b.UsuarioId)
                        .ToListAsync();

                    var billeteras = usuariosParaBilletera
                        .Where(u => !usuariosConBilletera.Contains(u.Id))
                        .Select(u => new Billetera
                        {
                            UsuarioId = u.Id,
                            SaldoTotal = 0,
                            SaldoRetenido = 0,
                            Version = Guid.NewGuid()
                        })
                        .ToList();

                    if (billeteras.Any())
                    {
                        context.Billeteras.AddRange(billeteras);
                        await context.SaveChangesAsync();
                    }
                }

                var listaUsuarios = await context.Usuarios
                    .Where(u =>
                        u.Email == "vendedor@test.com" ||
                        u.Email == "comprador1@test.com" ||
                        u.Email == "comprador2@test.com" ||
                        u.Email == "sinfondos@test.com")
                    .OrderBy(u => u.Id)
                    .ToListAsync();

                // 4. Subastas de prueba
                if (!await context.Subastas.AnyAsync() && listaUsuarios.Any())
                {
                    var listaCategorias = await context.Categorias.ToListAsync();
                    var vendedor = listaUsuarios.FirstOrDefault(u => u.Email == "vendedor@test.com") ?? listaUsuarios.First();

                    var subastas = new List<Subasta>
                    {
                        // Caso 1: Activa estándar (Cierra en 30 min)
                        new Subasta { VendedorId = vendedor.Id, CategoriaId = listaCategorias[0].Id, Titulo = "Casco VR Edición Coleccionista", Descripcion = "Casco de realidad virtual tope de gama.", UrlImagen = listaCategorias[0].ImagenUrl, PrecioBase = 10000, IncrementoMinimo = 2000, FechaInicio = DateTime.UtcNow.AddMinutes(-30), FechaFin = DateTime.UtcNow.AddMinutes(30), Estado = EstadoSubasta.Activa },

                        // Caso 2: Activa crítica (Cierra en menos de 2 min - Para Anti-Sniping)
                        new Subasta { VendedorId = vendedor.Id, CategoriaId = listaCategorias[1].Id, Titulo = "Consola Retro Edición Limitada", Descripcion = "Consola clásica de colección.", UrlImagen = listaCategorias[1].ImagenUrl, PrecioBase = 50000, IncrementoMinimo = 5000, FechaInicio = DateTime.UtcNow.AddMinutes(-58), FechaFin = DateTime.UtcNow.AddMinutes(1), Estado = EstadoSubasta.Activa },

                        // Caso 3: Próxima (Inicia a futuro)
                        new Subasta { VendedorId = vendedor.Id, CategoriaId = listaCategorias[3].Id, Titulo = "Llave NFT Prototipo Deportivo", Descripcion = "Acceso exclusivo a vehículo digital.", UrlImagen = listaCategorias[3].ImagenUrl, PrecioBase = 100000, IncrementoMinimo = 10000, FechaInicio = DateTime.UtcNow.AddHours(24), FechaFin = DateTime.UtcNow.AddHours(48), Estado = EstadoSubasta.Programada },

                        // Caso 4: Vencida con ganador
                        new Subasta { VendedorId = vendedor.Id, CategoriaId = listaCategorias[2].Id, Titulo = "Render Abstracto Ciberpunk #3", Descripcion = "Obra de arte digital 3D.", UrlImagen = listaCategorias[2].ImagenUrl, PrecioBase = 15000, IncrementoMinimo = 1000, FechaInicio = DateTime.UtcNow.AddHours(-3), FechaFin = DateTime.UtcNow.AddMinutes(-10), Estado = EstadoSubasta.Activa },

                        // Caso 5: Vencida desierta
                        new Subasta { VendedorId = vendedor.Id, CategoriaId = listaCategorias[0].Id, Titulo = "Cable HDMI Dañado", Descripcion = "Objeto sin uso.", UrlImagen = listaCategorias[0].ImagenUrl, PrecioBase = 1000, IncrementoMinimo = 100, FechaInicio = DateTime.UtcNow.AddHours(-5), FechaFin = DateTime.UtcNow.AddHours(-2), Estado = EstadoSubasta.Activa }
                    };

                    context.Subastas.AddRange(subastas);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DbInitializer] Error al aplicar seed: {ex.GetType().Name} - {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
        }
    }
}