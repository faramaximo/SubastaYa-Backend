using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditoriaRepository _auditoriaRepository;
    private readonly IDbContextFactory<SubastaYaDbContext> _contextFactory;
    private readonly ILogger<AuditService>? _logger;

    public AuditService(
        IAuditoriaRepository auditoriaRepository,
        IDbContextFactory<SubastaYaDbContext> contextFactory,
        ILogger<AuditService>? logger = null)
    {
        _auditoriaRepository = auditoriaRepository;
        _contextFactory = contextFactory;
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

    /// <summary>
    /// Crea un DbContext efímero e independiente para que el INSERT de auditoría
    /// se confirme en su propia conexión/transacción, sin ser afectado por el
    /// rollback de la transacción principal del handler que invoca este método.
    /// </summary>
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
            // Crear un DbContext nuevo e independiente del Scoped principal.
            // Esto garantiza una conexión y transacción separadas.
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var log = new AuditoriaLog
            {
                Entidad = entidad,
                EntidadId = entidadId,
                Accion = accion,
                UsuarioId = usuarioId,
                DetalleJson = detalleJson,
                Fecha = DateTime.UtcNow
            };

            context.AuditoriasLog.Add(log);
            await context.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation("Evento de auditoría persistido inmediatamente: {Accion} sobre {Entidad} #{EntidadId}",
                accion, entidad, entidadId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error al registrar y confirmar evento de auditoría inmediato: {Accion}", accion);
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

