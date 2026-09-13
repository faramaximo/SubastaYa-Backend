namespace SubastaYa.Application.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Prepara un evento de auditoría en el contexto actual sin confirmarlo en la base de datos.
    /// La confirmación atómica se realiza a través de IUnitOfWork.SaveChangesAsync().
    /// </summary>
    Task RegistrarEventoAsync(
        string entidad,
        int entidadId,
        string accion,
        int? usuarioId,
        string detalleJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prepara un evento de auditoría serializando automáticamente el objeto de detalle a JSON.
    /// </summary>
    Task RegistrarEventoAsync(
        string entidad,
        int entidadId,
        string accion,
        int? usuarioId,
        object detalle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra y confirma inmediatamente un evento de auditoría de forma atómica e independiente.
    /// Indicado para middlewares (ej. registro de colisiones de concurrencia) o procesos desacoplados.
    /// </summary>
    Task RegistrarYConfirmarEventoAsync(
        string entidad,
        int entidadId,
        string accion,
        int? usuarioId,
        string detalleJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra y confirma inmediatamente un evento de auditoría serializando el objeto de detalle.
    /// </summary>
    Task RegistrarYConfirmarEventoAsync(
        string entidad,
        int entidadId,
        string accion,
        int? usuarioId,
        object detalle,
        CancellationToken cancellationToken = default);
}
