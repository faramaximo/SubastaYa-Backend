using Microsoft.EntityFrameworkCore;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.WebApi.Workers
{
    public class AuctionStatusWorker : BackgroundService
    {
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

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<SubastaYaDbContext>();

                        // 1. ARRANCAR SUBASTAS: De Programada a Activa
                        var subastasParaActivar = await context.Subastas
                            .Where(s => s.Estado == EstadoSubasta.Programada && s.FechaInicio <= DateTime.UtcNow && s.FechaFin > DateTime.UtcNow)
                            .ToListAsync(stoppingToken);

                        foreach (var subasta in subastasParaActivar)
                        {
                            subasta.IniciarSubastaProgramada(); // Usamos el método de dominio
                            _logger.LogInformation($"🟢 Subasta {subasta.Id} ha comenzado. Estado cambiado a ACTIVA.");
                        }

                        // 2. CERRAR SUBASTAS VENCIDAS
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
                                subasta.FinalizarConGanador(); // Usamos el método de dominio

                                var billeteraComprador = await context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == pujaGanadora.CompradorId, stoppingToken);
                                var billeteraVendedor = await context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == subasta.VendedorId, stoppingToken);

                                if (billeteraComprador != null && billeteraVendedor != null)
                                {
                                    billeteraComprador.ProcesarPagoSubasta(pujaGanadora.Monto);
                                    billeteraVendedor.Depositar(pujaGanadora.Monto);
                                }
                                _logger.LogInformation($"✅ Subasta {subasta.Id} FINALIZADA. Ganador: {pujaGanadora.CompradorId}");
                            }
                            else
                            {
                                subasta.DeclararDesierta(); // Usamos el método de dominio
                                _logger.LogInformation($"👻 Subasta {subasta.Id} declarada DESIERTA.");
                            }
                        }

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

                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}