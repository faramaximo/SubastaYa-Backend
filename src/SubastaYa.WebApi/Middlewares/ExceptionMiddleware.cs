// SubastaYa.WebApi/Middlewares/ExceptionMiddleware.cs
using SubastaYa.Domain.Exceptions;

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

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (DomainException ex)              // regla de negocio violada
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            catch (Exception ex)                    // algo que no previmos
            {
                _logger.LogError(ex, "Error no controlado");
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await ctx.Response.WriteAsJsonAsync(new { error = "Error interno" });
            }
        }
    }
}