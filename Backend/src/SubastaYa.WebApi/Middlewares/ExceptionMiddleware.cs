using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Exceptions;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace SubastaYa.WebApi.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Colisión de concurrencia detectada en EF Core (RowVersion/xmin).");

            var usuarioId = ExtraerUsuarioId(context);
            var subastaId = ExtraerSubastaId(context);

            var detalleColision = new
            {
                mensaje = "Colisión de concurrencia detectada por pujas simultáneas sobre la misma subasta.",
                ruta = context.Request.Path.Value,
                metodo = context.Request.Method,
                subastaId = subastaId,
                usuarioId = usuarioId,
                entidadesEnConflicto = ex.Entries.Select(e => new
                {
                    entidad = e.Metadata.Name,
                    estado = e.State.ToString(),
                    claves = e.Properties.Where(p => p.Metadata.IsPrimaryKey()).Select(p => new { p.Metadata.Name, p.CurrentValue })
                }),
                errorOriginal = ex.Message,
                timestamp = DateTime.UtcNow
            };

            await auditService.RegistrarYConfirmarEventoAsync(
                entidad: "Subasta",
                entidadId: subastaId ?? 0,
                accion: "CONFLICTO_CONCURRENCIA_PUJA",
                usuarioId: usuarioId,
                detalle: detalleColision
            );

            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                statusCode = 409,
                message = "Alguien más realizó una puja al mismo tiempo. Actualizá la subasta y volvé a intentarlo."
            }));
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Colisión de concurrencia a nivel de dominio.");

            var usuarioId = ExtraerUsuarioId(context);
            var subastaId = ExtraerSubastaId(context);

            var detalleColision = new
            {
                mensaje = ex.Message,
                ruta = context.Request.Path.Value,
                metodo = context.Request.Method,
                subastaId = subastaId,
                usuarioId = usuarioId,
                timestamp = DateTime.UtcNow
            };

            await auditService.RegistrarYConfirmarEventoAsync(
                entidad: "Subasta",
                entidadId: subastaId ?? 0,
                accion: "CONFLICTO_CONCURRENCIA_PUJA",
                usuarioId: usuarioId,
                detalle: detalleColision
            );

            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                statusCode = 409,
                message = ex.Message
            }));
        }
        catch (UnauthorizedException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                statusCode = 401,
                message = ex.Message
            }));
        }
        catch (DomainException ex)
        {
            // Siguiendo el patrón del middleware expuesto en las diapositivas de la cátedra: HTTP 400 Bad Request
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                statusCode = 400,
                message = ex.Message
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                statusCode = 500,
                message = "Ocurrió un error interno en el servidor."
            }));
        }
    }

    private static int? ExtraerUsuarioId(HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out var id)) return id;
        return null;
    }

    private static int? ExtraerSubastaId(HttpContext context)
    {
        if (context.Request.RouteValues.TryGetValue("subastaId", out var val) ||
            context.Request.RouteValues.TryGetValue("id", out val))
        {
            if (int.TryParse(val?.ToString(), out var id)) return id;
        }
        return null;
    }
}