using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.Services;
using SubastaYa.Infrastructure.Data;
using SubastaYa.WebApi.Hubs;
using SubastaYa.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. Obtener cadena de conexión desde appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Registrar DbContext con MySQL
builder.Services.AddDbContext<SubastaYaDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 3. Registrar Inyección de Dependencias
builder.Services.AddScoped<IAuctionService, AuctionService>();
builder.Services.AddScoped<IWalletService, WalletService>(); 
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddSignalR();

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();

// 4. Intentar aplicar migraciones y cargar datos semilla en la Base de Datos con reintentos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 10;
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var context = services.GetRequiredService<SubastaYaDbContext>();
            // Intentar aplicar migraciones
            await context.Database.MigrateAsync();
            // Luego ejecutar seed (no volverá a migrar)
            await SubastaYa.Infrastructure.Seed.DbInitializer.SeedAsync(context);
            logger.LogInformation("Migraciones y seed aplicados correctamente.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Intento {Attempt} de aplicar migraciones/seed falló. Esperando antes de reintentar.", attempt);
            if (attempt == maxRetries)
            {
                logger.LogError(ex, "No se pudieron aplicar las migraciones tras {Max} intentos.", maxRetries);
                throw;
            }

            // Espera exponencial (con tope razonable)
            var delaySeconds = Math.Min(30, 2 * attempt);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }
}

// 5. Configurar Middlewares y Archivos Estáticos (Frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<AuctionHub>("/hubs/auction");

app.Run();