using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.Services;
using SubastaYa.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Obtener cadena de conexión desde appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Registrar DbContext
builder.Services.AddDbContext<SubastaYaDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));


// 🔴 2. REGISTRAR AQUÍ LOS SERVICIOS (Inyección de Dependencias)
builder.Services.AddScoped<IAuctionService, AuctionService>();

builder.Services.AddControllers();

// Encendemos el proceso en segundo plano (Background Worker)
builder.Services.AddHostedService<SubastaYa.WebApi.Workers.AuctionStatusWorker>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();



// 🔴 IMPORTANTE: Habilita el uso de archivos estáticos (HTML, CSS, JS) desde la carpeta wwwroot
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
// ============================================================
// DISPARADOR DEL SEEDER AL ARRANCAR LA API
// Esto ejecuta DbInitializer cada vez que apretás F5
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SubastaYaDbContext>();
        // Llama a nuestro seeder (Asegurate de tener el using de tu clase DbInitializer arriba si hace falta)
        await SubastaYa.Infrastructure.Seed.DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ocurrió un error al ejecutar el Seeder: {ex.Message}");
    }
}

app.Run();
