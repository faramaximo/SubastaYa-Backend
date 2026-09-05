using Microsoft.EntityFrameworkCore;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.WebApi.Workers
{
    public class AuctionStatusWorker : BackgroundService
    {
        // Usamos IServiceProvider porque el Worker vive para siempre (Singleton), 
        // pero la base de datos (DbContext) nace y muere en cada petición (Scoped).
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuctionStatusWorker> _logger;

        public AuctionStatusWorker(IServiceProvider serviceProvider, ILogger<AuctionStatusWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🤖 Worker de Subastas iniciado.");

            // El ciclo infinito que mantiene vivo al worker
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Creamos un alcance (scope) para poder usar la Base de Datos con seguridad
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<SubastaYaDbContext>();

                        // 1. ARRANCAR SUBASTAS: De Programada a Activa
                        var subastasParaActivar = await context.Subastas
                            .Where(s => s.Estado == EstadoSubasta.Programada && s.FechaInicio <= DateTime.UtcNow && s.FechaFin > DateTime.UtcNow)
                            .ToListAsync(stoppingToken);

                        foreach (var subasta in subastasParaActivar)
                        {
                            subasta.Estado = EstadoSubasta.Activa;
                            _logger.LogInformation($"🟢 Subasta {subasta.Id} ha comenzado. Estado cambiado a ACTIVA.");
                        }

                        // 2. CERRAR SUBASTAS VENCIDAS (Fueran Activas o Programadas con error)
                        var subastasVencidas = await context.Subastas
                            .Where(s => (s.Estado == EstadoSubasta.Activa || s.Estado == EstadoSubasta.Programada) && s.FechaFin <= DateTime.UtcNow)
                            .ToListAsync(stoppingToken);

                        foreach (var subasta in subastasVencidas)
                        {
                            var pujaGanadora = await context.Pujas
                                .Where(p => p.SubastaId == subasta.Id)
                                .OrderByDescending(p => p.Monto)
                                .FirstOrDefaultAsync(stoppingToken);

                            if (pujaGanadora != null)
                            {
                                subasta.Estado = EstadoSubasta.Finalizada;
                                var billeteraComprador = await context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == pujaGanadora.CompradorId, stoppingToken);
                                var billeteraVendedor = await context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == subasta.VendedorId, stoppingToken);

                                if (billeteraComprador != null && billeteraVendedor != null)
                                {
                                    // El worker ya no hace cuentas, solo delega la responsabilidad a las entidades[cite: 1]
                                    billeteraComprador.ProcesarPagoSubasta(pujaGanadora.Monto);
                                    billeteraVendedor.Depositar(pujaGanadora.Monto);
                                }
                                _logger.LogInformation($"✅ Subasta {subasta.Id} FINALIZADA. Ganador: {pujaGanadora.CompradorId}");
                            }
                            else
                            {
                                subasta.Estado = EstadoSubasta.Desierta;
                                _logger.LogInformation($"👻 Subasta {subasta.Id} declarada DESIERTA.");
                            }
                        }

                        // Guardamos todos los cambios juntos
                        if (subastasParaActivar.Any() || subastasVencidas.Any())
                        {
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Ocurrió un error en el worker de liquidación.");
                }

                // El worker se va a dormir 10 segundos antes de volver a revisar la base de datos
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}