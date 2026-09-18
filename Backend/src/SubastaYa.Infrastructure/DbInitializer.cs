using Microsoft.EntityFrameworkCore;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.Infrastructure.Seed
{
    public static class DbInitializer
    {
        public static Task InitializeAsync(SubastaYaDbContext context) => SeedAsync(context);

        public static async Task SeedAsync(SubastaYaDbContext context)
        {
            try
            {
                await context.Database.MigrateAsync();

                const string defaultSeedPassword = "123456";

                // 1. Si no hay usuarios, sembramos TODO el conjunto inicial
                if (!await context.Usuarios.AnyAsync())
                {
                    // ====================================================================
                    // 1. CATEGORÍAS OBLIGATORIAS
                    // ====================================================================
                    var categorias = new List<Categoria>
                    {
                        new Categoria { Nombre = "Tecnología", ImagenUrl = "https://images.unsplash.com/photo-1550745165-9bc0b252726f" },
                        new Categoria { Nombre = "Coleccionables", ImagenUrl = "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f" },
                        new Categoria { Nombre = "Indumentaria", ImagenUrl = "https://fund.ar/wp-content/uploads/2023/12/pexels-rdne-stock-project-5698851-scaled.jpg" },
                        new Categoria { Nombre = "Vehículos", ImagenUrl = "https://images.unsplash.com/photo-1552519507-da3b142c6e3d" }
                    };
                    context.Categorias.AddRange(categorias);
                    await context.SaveChangesAsync();

                    // ====================================================================
                    // 2. USUARIOS Y BILLETERAS OBLIGATORIAS
                    // ====================================================================
                    string passwordHasheada = BCrypt.Net.BCrypt.HashPassword(defaultSeedPassword);

                    var usuarios = new List<Usuario>
                    {
                        new Usuario { Nombre = "Vendedor Test", Email = "vendedor@test.com", PasswordHash = passwordHasheada, FechaRegistro = DateTime.UtcNow, EmailVerificado = true },
                        new Usuario { Nombre = "Comprador Líder", Email = "comprador1@test.com", PasswordHash = passwordHasheada, FechaRegistro = DateTime.UtcNow, EmailVerificado = true },
                        new Usuario { Nombre = "Comprador Dos", Email = "comprador2@test.com", PasswordHash = passwordHasheada, FechaRegistro = DateTime.UtcNow, EmailVerificado = true },
                        new Usuario { Nombre = "Usuario Sin Fondos", Email = "sinfondos@test.com", PasswordHash = passwordHasheada, FechaRegistro = DateTime.UtcNow, EmailVerificado = true }
                    };

                    context.Usuarios.AddRange(usuarios);
                    await context.SaveChangesAsync();

                    var billeteras = new List<Billetera>
                    {
                        new Billetera(usuarios[0].Id, 0, 0),
                        new Billetera(usuarios[1].Id, 150000, 45000),
                        new Billetera(usuarios[2].Id, 200000, 18000),
                        new Billetera(usuarios[3].Id, 500, 0)
                    };
                    context.Billeteras.AddRange(billeteras);
                    await context.SaveChangesAsync();

                    // ====================================================================
                    // 3. LAS 5 SUBASTAS DEL TP
                    // ====================================================================
                    var subastas = new List<Subasta>
                    {
                        // 0: Activa estándar
                        new Subasta(usuarios[0].Id, categorias[3].Id, "Ford Fiesta", "Buen estado", "https://images.unsplash.com/photo-1552519507-da3b142c6e3d", 15000m, 500m, DateTime.UtcNow.AddMinutes(25)),
                            
                        // 1: Activa crítica (anti-sniping)
                        new Subasta(usuarios[0].Id, categorias[0].Id, "Laptop Gamer", "Casi nueva", "https://es.digitaltrends.com/tachyon/sites/13/2023/09/lenovo-legion-pro-5.jpg?resize=800%2C418", 80000m, 1000m, DateTime.UtcNow.AddSeconds(120)),
                            
                        // 2: Próxima (Programada +24 hs)
                        new Subasta(usuarios[0].Id, categorias[1].Id, "Reloj Antiguo", "Siglo XIX", "https://i.pinimg.com/736x/4b/a1/e3/4ba1e341b9f2e8c1b3d023c2a9297fc6.jpg", 50000m, 2000m, DateTime.UtcNow.AddHours(48)),
                            
                        // 3: Vencida con ganador
                        new Subasta(usuarios[0].Id, categorias[2].Id, "Chaqueta de Cuero Vintage", "Edición limitada", "https://images.stockcake.com/public/b/6/1/b61db703-ace3-4bc4-a121-cf076f7f7de1/vintage-leather-jacket-stockcake.jpg", 10000m, 1000m, DateTime.UtcNow.AddMinutes(10)),
                            
                        // 4: Vencida desierta
                        new Subasta(usuarios[0].Id, categorias[0].Id, "Auriculares In-Ear", "Inalámbricos", "https://http2.mlstatic.com/D_NQ_NP_855203-MLA99567193754_122025-O.webp", 5000m, 100m, DateTime.UtcNow.AddMinutes(10))
                    };

                    context.Subastas.AddRange(subastas);
                    await context.SaveChangesAsync();

                    // Ajuste explícito de fechas y estados
                    context.Entry(subastas[2]).Property(s => s.FechaInicio).CurrentValue = DateTime.UtcNow.AddHours(24);
                    context.Entry(subastas[2]).Property(s => s.Estado).CurrentValue = EstadoSubasta.Programada;

                    context.Entry(subastas[3]).Property(s => s.FechaFin).CurrentValue = DateTime.UtcNow.AddMinutes(-5);
                    context.Entry(subastas[4]).Property(s => s.FechaFin).CurrentValue = DateTime.UtcNow.AddHours(-2);

                    await context.SaveChangesAsync();

                    // ====================================================================
                    // 4. PUJAS OBLIGATORIAS
                    // ====================================================================
                    var pujas = new List<Puja>
                    {
                        new Puja { SubastaId = subastas[0].Id, CompradorId = usuarios[2].Id, Monto = 20000, FechaPuja = DateTime.UtcNow.AddMinutes(-20) },
                        new Puja { SubastaId = subastas[0].Id, CompradorId = usuarios[1].Id, Monto = 45000, FechaPuja = DateTime.UtcNow.AddMinutes(-10) },
                        new Puja { SubastaId = subastas[3].Id, CompradorId = usuarios[2].Id, Monto = 18000, FechaPuja = DateTime.UtcNow.AddHours(-1) }
                    };
                    context.Pujas.AddRange(pujas);

                    // ====================================================================
                    // 5. LEDGER CONCILIADO
                    // ====================================================================
                    var transacciones = new List<TransaccionLedger>
                    {
                        new TransaccionLedger { BilleteraId = billeteras[1].Id, Tipo = TipoTransaccion.Deposito, Monto = 150000m, Fecha = DateTime.UtcNow.AddMinutes(-30) },
                        new TransaccionLedger { BilleteraId = billeteras[1].Id, Tipo = TipoTransaccion.Retencion, Monto = 45000m, Fecha = DateTime.UtcNow.AddMinutes(-10), SubastaId = subastas[0].Id },
                        new TransaccionLedger { BilleteraId = billeteras[2].Id, Tipo = TipoTransaccion.Deposito, Monto = 200000m, Fecha = DateTime.UtcNow.AddMinutes(-30) },
                        new TransaccionLedger { BilleteraId = billeteras[2].Id, Tipo = TipoTransaccion.Retencion, Monto = 20000m, Fecha = DateTime.UtcNow.AddMinutes(-20), SubastaId = subastas[0].Id },
                        new TransaccionLedger { BilleteraId = billeteras[2].Id, Tipo = TipoTransaccion.Liberacion, Monto = 20000m, Fecha = DateTime.UtcNow.AddMinutes(-10), SubastaId = subastas[0].Id },
                        new TransaccionLedger { BilleteraId = billeteras[2].Id, Tipo = TipoTransaccion.Retencion, Monto = 18000m, Fecha = DateTime.UtcNow.AddHours(-1), SubastaId = subastas[3].Id },
                        new TransaccionLedger { BilleteraId = billeteras[3].Id, Tipo = TipoTransaccion.Deposito, Monto = 500m, Fecha = DateTime.UtcNow.AddMinutes(-30) }
                    };
                    context.TransaccionesLedger.AddRange(transacciones);

                    await context.SaveChangesAsync();
                    Console.WriteLine("--> [SEED SUCCESS] Base de datos sembrada con 5 subastas y conciliación contable.");
                }
                else
                {
                    // Si ya existen usuarios, actualizamos contraseñas si fuera necesario
                    var usuariosSinHash = await context.Usuarios
                        .Where(u => string.IsNullOrEmpty(u.PasswordHash) || !u.PasswordHash.StartsWith("$2"))
                        .ToListAsync();

                    if (usuariosSinHash.Count > 0)
                    {
                        foreach (var u in usuariosSinHash)
                        {
                            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultSeedPassword);
                        }
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> [SEED ERROR] Ocurrió una excepción al sembrar la base de datos: {ex.Message}");
                throw;
            }
        }
    }
}
