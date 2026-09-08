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
            catch (SubastaYa.Domain.Exceptions.UnauthorizedException ex)
            {
                // Errores de autenticación deben mapear a 401 Unauthorized
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";

                var response = new { error = ex.Message };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (DomainException ex)
            {
                // Atrapa la regla de negocio y devuelve HTTP 400 Bad Request prolijo
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                var response = new { error = ex.Message };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response)); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var response = new { error = "Ocurrió un error interno en el servidor." };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}
