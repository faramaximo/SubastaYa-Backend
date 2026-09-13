using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Data;
using SubastaYa.WebApi.Hubs; // Asegúrate de incluir el namespace de tu Hub

namespace SubastaYa.WebApi.Workers
{
    public class AuctionStatusWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuctionStatusWorker> _logger;
        private readonly IHubContext<AuctionHub> _hubContext; // 1. Declarar el contexto del Hub

        // 2. Inyectar IHubContext en el constructor
        public AuctionStatusWorker(
            IServiceProvider serviceProvider,
            ILogger<AuctionStatusWorker> logger,
            IHubContext<AuctionHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hubContext = hubContext;
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
                        bool huboCambios = false;

                        // 1. ARRANCAR SUBASTAS: De Programada a Activa
                        var subastasParaActivar = await context.Subastas
                            .Where(s => s.Estado == EstadoSubasta.Programada && s.FechaInicio <= DateTime.UtcNow && s.FechaFin > DateTime.UtcNow)
                            .ToListAsync(stoppingToken);

                        foreach (var subasta in subastasParaActivar)
                        {
                            subasta.IniciarSubastaProgramada();
                            _logger.LogInformation($"🟢 Subasta {subasta.Id} ha comenzado. Estado cambiado a ACTIVA.");

                            // Avisar a la sala por si alguien está esperando que inicie
                            await _hubContext.Clients.Group($"subasta-{subasta.Id}")
                                .SendAsync("SubastaIniciada", subasta.Id, cancellationToken: stoppingToken);

                            huboCambios = true;
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
                                subasta.FinalizarConGanador();

                                var billeteraComprador = await context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == pujaGanadora.CompradorId, stoppingToken);
                                var billeteraVendedor = await context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == subasta.VendedorId, stoppingToken);

                                if (billeteraComprador != null && billeteraVendedor != null)
                                {
                                    billeteraComprador.ProcesarPagoSubasta(pujaGanadora.Monto);
                                    billeteraVendedor.Depositar(pujaGanadora.Monto);
                                }
                                _logger.LogInformation($"✅ Subasta {subasta.Id} FINALIZADA. Ganador: {pujaGanadora.CompradorId}");

                                // Avisar a los usuarios dentro de la sala con el prefijo correcto
                                await _hubContext.Clients.Group($"subasta-{subasta.Id}").SendAsync("SubastaCerrada", new
                                {
                                    SubastaId = subasta.Id, // Añadido para que el JS sepa de cuál hablamos
                                    GanadorId = pujaGanadora.CompradorId,
                                    MontoFinal = pujaGanadora.Monto
                                }, cancellationToken: stoppingToken);
                            }
                            else
                            {
                                subasta.DeclararDesierta();
                                _logger.LogInformation($"👻 Subasta {subasta.Id} declarada DESIERTA.");

                                // Avisar que quedó desierta
                                await _hubContext.Clients.Group($"subasta-{subasta.Id}")
                                    .SendAsync("SubastaDesierta", subasta.Id, cancellationToken: stoppingToken);
                            }

                            huboCambios = true;
                        }

                        if (huboCambios)
                        {
                            await context.SaveChangesAsync(stoppingToken);

                            // ¡CRUCIAL! Avisar a todos los conectados para que el catálogo general recargue sus tarjetas
                            await _hubContext.Clients.All.SendAsync("SubastaActualizada", cancellationToken: stoppingToken);
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