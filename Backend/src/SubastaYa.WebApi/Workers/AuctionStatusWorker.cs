using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Data;
using SubastaYa.WebApi.Hubs; // Asegúrate de incluir el namespace de tu Hub

namespace SubastaYa.WebApi.Workers
{
    public class AuctionStatusWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuctionStatusWorker> _logger;

        public AuctionStatusWorker(
            IServiceProvider serviceProvider,
            ILogger<AuctionStatusWorker> logger)
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
                        var notifier = scope.ServiceProvider.GetRequiredService<IAuctionNotifier>(); // 👈 Obtenemos el notificador del scope
                        bool huboCambios = false;

                        // 1. ARRANCAR SUBASTAS
                        var subastasParaActivar = await context.Subastas
                            .Where(s => s.Estado == EstadoSubasta.Programada && s.FechaInicio <= DateTime.UtcNow && s.FechaFin > DateTime.UtcNow)
                            .ToListAsync(stoppingToken);

                        foreach (var subasta in subastasParaActivar)
                        {
                            subasta.IniciarSubastaProgramada();
                            _logger.LogInformation($"🟢 Subasta {subasta.Id} ha comenzado. Estado cambiado a ACTIVA.");

                            // Usamos la abstracción
                            await notifier.NotificarSubastaIniciadaAsync(subasta.Id);
                            huboCambios = true;
                        }

                        // 2. CERRAR SUBASTAS
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
                                subasta.FinalizarConGanador();

                                var billeteraComprador = await context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == pujaGanadora.CompradorId, stoppingToken);
                                var billeteraVendedor = await context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == subasta.VendedorId, stoppingToken);

                                if (billeteraComprador != null && billeteraVendedor != null)
                                {
                                    billeteraComprador.ProcesarPagoSubasta(pujaGanadora.Monto);
                                    billeteraVendedor.Depositar(pujaGanadora.Monto);
                                }
                                _logger.LogInformation($"✅ Subasta {subasta.Id} FINALIZADA. Ganador: {pujaGanadora.CompradorId}");

                                // Usamos la variable notifier del scope
                                await notifier.NotificarSubastaFinalizadaAsync(subasta.Id, pujaGanadora.CompradorId, pujaGanadora.Monto);
                            }
                            else
                            {
                                subasta.DeclararDesierta();
                                _logger.LogInformation($"👻 Subasta {subasta.Id} declarada DESIERTA.");

                                // Usamos la variable notifier del scope
                                await notifier.NotificarSubastaFinalizadaAsync(subasta.Id, null, 0);
                            }

                            huboCambios = true;
                        }

                        // 3. CONFIRMAR CAMBIOS Y REFRESCA CATÁLOGO
                        if (huboCambios)
                        {
                            await context.SaveChangesAsync(stoppingToken);

                            // Refrescamos de manera global
                            await notifier.NotificarActualizacionCatalogoAsync();
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