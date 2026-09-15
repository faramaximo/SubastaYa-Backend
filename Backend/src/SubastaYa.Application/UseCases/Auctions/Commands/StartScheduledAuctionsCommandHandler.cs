using SubastaYa.Application.Interfaces;

namespace SubastaYa.Application.UseCases.Auctions.Commands;

public record StartScheduledAuctionsCommand;

public class StartScheduledAuctionsCommandHandler
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuctionNotifier _notifier;
    private readonly IAuditService _auditService;

    public StartScheduledAuctionsCommandHandler(
        IAuctionRepository auctionRepository,
        IUnitOfWork unitOfWork,
        IAuctionNotifier notifier,
        IAuditService auditService)
    {
        _auctionRepository = auctionRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _auditService = auditService;
    }

    public async Task<List<int>> Handle(StartScheduledAuctionsCommand command, CancellationToken cancellationToken = default)
    {
        var ahoraUtc = DateTime.UtcNow;
        var subastasParaActivar = await _auctionRepository.ObtenerProgramadasParaIniciarAsync(ahoraUtc, cancellationToken);

        if (subastasParaActivar.Count == 0)
            return new List<int>();

        var subastasIniciadasIds = new List<int>();

        foreach (var subasta in subastasParaActivar)
        {
            subasta.IniciarSubastaProgramada();

            await _auditService.RegistrarEventoAsync(
                entidad: "Subasta",
                entidadId: subasta.Id,
                accion: "SUBASTA_INICIADA",
                usuarioId: null,
                detalle: new
                {
                    SubastaId = subasta.Id,
                    FechaInicio = subasta.FechaInicio,
                    FechaFin = subasta.FechaFin,
                    Motivo = "Transicion automatica de Programada a Activa por el worker."
                },
                cancellationToken: cancellationToken);

            await _notifier.NotificarSubastaIniciadaAsync(subasta.Id);
            subastasIniciadasIds.Add(subasta.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notifier.NotificarActualizacionCatalogoAsync();

        return subastasIniciadasIds;
    }
}
