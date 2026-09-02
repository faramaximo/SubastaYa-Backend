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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//cargar los datos a la bd.

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SubastaYaDbContext>();
        await SubastaYa.Infrastructure.Seed.DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al poblar la base de datos con los datos semilla.");
    }
}




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

app.Run();