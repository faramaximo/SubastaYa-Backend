using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Exceptions;
using System.Net;
using System.Security.Claims;

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
            _logger.LogWarning(ex, "Colisión de concurrencia detectada en EF Core.");
            await RegistrarConflictoAsync(context, auditService, ex.Message);
            await EscribirProblemaAsync(context, HttpStatusCode.Conflict,
                "Conflicto de concurrencia",
                "La subasta fue modificada por otro usuario. Por favor intente nuevamente.");
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Colisión de concurrencia a nivel de dominio.");
            await RegistrarConflictoAsync(context, auditService, ex.Message);
            await EscribirProblemaAsync(context, HttpStatusCode.Conflict, "Conflicto de concurrencia", "La subasta fue modificada por otro usuario. Por favor intente nuevamente.");
        }
        catch (UnauthorizedException ex)
        {
            await EscribirProblemaAsync(context, HttpStatusCode.Unauthorized, "No autenticado", ex.Message);
        }
        catch (ForbiddenException ex)
        {
            await EscribirProblemaAsync(context, HttpStatusCode.Forbidden, "Acción prohibida", ex.Message);
        }
        catch (ResourceNotFoundException ex)
        {
            await EscribirProblemaAsync(context, HttpStatusCode.NotFound, "Recurso no encontrado", ex.Message);
        }
        catch (DomainException ex)
        {
            // El JSON es válido: la petición no puede procesarse por una regla del dominio.
            await EscribirProblemaAsync(context, HttpStatusCode.UnprocessableEntity,
                "Regla de negocio no satisfecha", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado");
            await EscribirProblemaAsync(context, HttpStatusCode.InternalServerError,
                "Error interno del servidor", "Ocurrió un error interno en el servidor.");
        }
    }

    private static async Task EscribirProblemaAsync(HttpContext context, HttpStatusCode status, string title, string detail)
    {
        if (context.Response.HasStarted)
            return;

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static async Task RegistrarConflictoAsync(HttpContext context, IAuditService auditService, string mensaje)
    {
        var usuarioId = ExtraerUsuarioId(context);
        var subastaId = ExtraerSubastaId(context);

        try
        {
            await auditService.RegistrarYConfirmarEventoAsync(
                entidad: "Subasta",
                entidadId: subastaId ?? 0,
                accion: "CONFLICTO_CONCURRENCIA_PUJA",
                usuarioId: usuarioId,
                detalle: new
                {
                    mensaje,
                    ruta = context.Request.Path.Value,
                    metodo = context.Request.Method,
                    subastaId,
                    usuarioId,
                    timestamp = DateTime.UtcNow
                });
        }
        catch (Exception auditException)
        {
            // El fallo de auditoría no debe ocultar el 409 que recibió el cliente.
            context.RequestServices.GetRequiredService<ILogger<ExceptionMiddleware>>()
                .LogError(auditException, "No se pudo auditar el conflicto de concurrencia.");
        }
    }

    private static int? ExtraerUsuarioId(HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static int? ExtraerSubastaId(HttpContext context)
    {
        if (context.Request.RouteValues.TryGetValue("auctionId", out var val) ||
            context.Request.RouteValues.TryGetValue("subastaId", out val) ||
            context.Request.RouteValues.TryGetValue("id", out val))
        {
            return int.TryParse(val?.ToString(), out var id) ? id : null;
        }

        return null;
    }
}