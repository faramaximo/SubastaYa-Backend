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
                        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
                        bool huboCambios = false;

                        // 1. ARRANCAR SUBASTAS PROGRAMADAS
                        var subastasParaActivar = await context.Subastas
                            .Where(s => s.Estado == EstadoSubasta.Programada && s.FechaInicio <= DateTime.UtcNow && s.FechaFin > DateTime.UtcNow)
                            .ToListAsync(stoppingToken);

                        foreach (var subasta in subastasParaActivar)
                        {
                            subasta.IniciarSubastaProgramada();
                            _logger.LogInformation($"🟢 Subasta {subasta.Id} ha comenzado. Estado cambiado a ACTIVA.");

                            await auditService.RegistrarEventoAsync(
                                entidad: "Subasta",
                                entidadId: subasta.Id,
                                accion: "SUBASTA_INICIADA",
                                usuarioId: null,
                                detalle: new
                                {
                                    subastaId = subasta.Id,
                                    estado = EstadoSubasta.Activa.ToString(),
                                    fechaInicioReal = DateTime.UtcNow
                                },
                                cancellationToken: stoppingToken
                            );

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
                            .Where(s => (s.Estado == EstadoSubasta.Activa || s.Estado == EstadoSubasta.Programada) && s.FechaFin <= DateTime.UtcNow)
                            .Select(s => s.Id)
                            .ToListAsync(stoppingToken);

                        // 3. LIQUIDAR CADA SUBASTA MEDIANTE EL HANDLER DENTRO DE SU PROPIO SCOPE
                        foreach (var subastaId in subastasVencidasIds)
                        {
                            try
                            {
                                using var finalizeScope = _scopeFactory.CreateScope();
                                var finalizeHandler = finalizeScope.ServiceProvider.GetRequiredService<FinalizeAuctionCommandHandler>();
                                await finalizeHandler.Handle(new FinalizeAuctionCommand(subastaId), stoppingToken);
                                _logger.LogInformation($"🏁 Subasta {subastaId} procesada por FinalizeAuctionCommandHandler.");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"❌ Error liquidando subasta {subastaId}.");
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