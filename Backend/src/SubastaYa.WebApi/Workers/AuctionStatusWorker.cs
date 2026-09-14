using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auctions.Commands;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.WebApi.Workers
{
    public class AuctionStatusWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuctionStatusWorker> _logger;

        public AuctionStatusWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<AuctionStatusWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🤖 Worker de Subastas iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<SubastaYaDbContext>();
                        var notifier = scope.ServiceProvider.GetRequiredService<IAuctionNotifier>();
                        bool huboCambios = false;
                        var ahoraUtc = DateTime.UtcNow;

                        // 1. ARRANCAR SUBASTAS PROGRAMADAS
                        var subastasParaActivar = await context.Subastas
                            .Where(s => s.Estado == EstadoSubasta.Programada && s.FechaInicio <= ahoraUtc && s.FechaFin > ahoraUtc)
                            .ToListAsync(stoppingToken);

                        foreach (var subasta in subastasParaActivar)
                        {
                            subasta.IniciarSubastaProgramada();
                            _logger.LogInformation($"🟢 Subasta {subasta.Id} ha comenzado. Estado cambiado a ACTIVA.");

                            await notifier.NotificarSubastaIniciadaAsync(subasta.Id);
                            huboCambios = true;
                        }

                        if (huboCambios)
                        {
                            await context.SaveChangesAsync(stoppingToken);
                            await notifier.NotificarActualizacionCatalogoAsync();
                        }

                        // 2. OBTENER IDS DE SUBASTAS VENCIDAS PARA LIQUIDAR
                        var subastasVencidasIds = await context.Subastas
                            .Where(s => (s.Estado == EstadoSubasta.Activa || s.Estado == EstadoSubasta.Programada) && s.FechaFin <= ahoraUtc)
                            .Select(s => s.Id)
                            .ToListAsync(stoppingToken);

                        // 3. LIQUIDAR CADA SUBASTA MEDIANTE EL HANDLER DENTRO DE SU PROPIO SCOPE
                        foreach (var subastaId in subastasVencidasIds)
                        {
                            try
                            {
                                using var finalizeScope = _scopeFactory.CreateScope();
                                var finalizeHandler = finalizeScope.ServiceProvider.GetRequiredService<FinalizeAuctionCommandHandler>();
                                var fueLiquidada = await finalizeHandler.Handle(new FinalizeAuctionCommand(subastaId), stoppingToken);

                                if (fueLiquidada)
                                {
                                    _logger.LogInformation("🏁 Subasta {SubastaId} liquidada correctamente.", subastaId);
                                }
                                else
                                {
                                    _logger.LogDebug("La subasta {SubastaId} dejó de ser liquidable antes de adquirir el bloqueo de transacción.", subastaId);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "❌ Error liquidando subasta {SubastaId}.", subastaId);
                            }
                        }

                        if (subastasVencidasIds.Any())
                        {
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
