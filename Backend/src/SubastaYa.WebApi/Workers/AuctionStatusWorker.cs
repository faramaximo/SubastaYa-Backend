using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auctions.Commands;

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

            // Espera inicial para permitir que las migraciones y arranque de la BD concluyan
            try
            {
                await Task.Delay(2500, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var startHandler = scope.ServiceProvider.GetRequiredService<StartScheduledAuctionsCommandHandler>();
                        var auctionRepository = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
                        var notifier = scope.ServiceProvider.GetRequiredService<IAuctionNotifier>();
                        var ahoraUtc = DateTime.UtcNow;

                        // 1. ARRANCAR SUBASTAS PROGRAMADAS MEDIANTE EL HANDLER DE APLICACIÓN
                        var iniciadasIds = await startHandler.Handle(new StartScheduledAuctionsCommand(), stoppingToken);
                        foreach (var subastaId in iniciadasIds)
                        {
                            _logger.LogInformation("🟢 Subasta {SubastaId} ha comenzado. Estado cambiado a ACTIVA.", subastaId);
                        }

                        // 2. OBTENER IDS DE SUBASTAS VENCIDAS PARA LIQUIDAR A TRAVÉS DEL REPOSITORIO
                        var subastasVencidasIds = await auctionRepository.ObtenerIdsVencidasAsync(ahoraUtc, stoppingToken);

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
                                    _logger.LogDebug("La subasta {SubastaId} dejo de ser liquidable antes de adquirir el bloqueo de transaccion.", subastaId);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("⚠️ Error liquidando subasta {SubastaId}: {Message}", subastaId, ex.Message);
                            }
                        }

                        if (subastasVencidasIds.Any())
                        {
                            await notifier.NotificarActualizacionCatalogoAsync();
                        }
                    }
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning("⚠️ Base de datos no disponible o error transitorio en AuctionStatusWorker ({Message}). Reintentando en el próximo ciclo...", ex.Message);
                }

                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
