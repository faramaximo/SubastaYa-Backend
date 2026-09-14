using System.Text.Json;
using Microsoft.Extensions.Logging;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditoriaRepository _auditoriaRepository;
    private readonly SubastaYaDbContext _context;
    private readonly ILogger<AuditService>? _logger;

    public AuditService(
        IAuditoriaRepository auditoriaRepository,
        SubastaYaDbContext context,
        ILogger<AuditService>? logger = null)
    {
        _auditoriaRepository = auditoriaRepository;
        _context = context;
        _logger = logger;
    }

    public async Task RegistrarEventoAsync(
        string entidad,
        int entidadId,
        string accion,
        int? usuarioId,
        string detalleJson,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditoriaLog
        {
            Entidad = entidad,
            EntidadId = entidadId,
            Accion = accion,
            UsuarioId = usuarioId,
            DetalleJson = detalleJson,
            Fecha = DateTime.UtcNow
        };

        await _auditoriaRepository.AgregarAsync(log, cancellationToken);
        _logger?.LogInformation("Evento de auditoría preparado: {Accion} sobre {Entidad} #{EntidadId} (Usuario: {UsuarioId})",
            accion, entidad, entidadId, usuarioId);
    }

    public Task RegistrarEventoAsync(
        string entidad,
        int entidadId,
        string accion,
        int? usuarioId,
        object detalle,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(detalle);
        return RegistrarEventoAsync(entidad, entidadId, accion, usuarioId, json, cancellationToken);
    }

    public async Task RegistrarYConfirmarEventoAsync(
        string entidad,
        int entidadId,
        string accion,
        int? usuarioId,
        string detalleJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Limpiamos el ChangeTracker para evitar arrastrar entidades en conflicto (ej. DbUpdateConcurrencyException)
            _context.ChangeTracker.Clear();

            var log = new AuditoriaLog
            {
                Entidad = entidad,
                EntidadId = entidadId,
                Accion = accion,
                UsuarioId = usuarioId,
                DetalleJson = detalleJson,
                Fecha = DateTime.UtcNow
            };

            await _auditoriaRepository.AgregarAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation("Evento de auditoría persistido inmediatamente: {Accion} sobre {Entidad} #{EntidadId}",
                accion, entidad, entidadId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error al registrar y confirmar evento de auditoría inmediato: {Accion}", accion);
            throw; // Relanzar la excepción para garantizar integridad estricta (no fallar en silencio)
        }
    }

    public Task RegistrarYConfirmarEventoAsync(
        string entidad,
        int entidadId,
        string accion,
        int? usuarioId,
        object detalle,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(detalle);
        return RegistrarYConfirmarEventoAsync(entidad, entidadId, accion, usuarioId, json, cancellationToken);
    }
}
