using SubastaYa.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace SubastaYa.WebApi.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ConcurrencyException ex)
            {
                // 1. Hija de DomainException (409 Conflict)
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            catch (SubastaYa.Domain.Exceptions.UnauthorizedException ex)
            {
                // 2. Hija de DomainException (401 Unauthorized)
                // Lo movimos ARRIBA de DomainException
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            catch (DomainException ex)
            {
                // 3. Excepción Padre (422 Unprocessable Entity)
                // Atrapa cualquier otro error de negocio que no sea de concurrencia ni de autorización
                context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            catch (Exception ex)
            {
                // 4. Excepción genérica de C# (500 Internal Server Error)
                _logger.LogError(ex, "Error no controlado");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Ocurrió un error interno en el servidor." }));
            }
        }
    }
}