using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auctions.Handlers;
using SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Usuarios.Handlers;
using SubastaYa.Application.UseCases.Wallet.Handlers;
using SubastaYa.Infrastructure.Data;
using SubastaYa.Infrastructure.Persistence.Queries;
using SubastaYa.Infrastructure.Persistence.Repositories;
using SubastaYa.Infrastructure.Repositories;
using SubastaYa.WebApi.Middlewares;



var builder = WebApplication.CreateBuilder(args);

// 1. Obtener cadena de conexión desde appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Registrar DbContext con MySQL
builder.Services.AddDbContext<SubastaYaDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 3. Registrar Inyección de Dependencias
builder.Services.AddControllers();

// ── INFRASTRUCTURE: UnitOfWork, Repositorios y Queries ──
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>(); 

builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ILedgerRepository, LedgerRepository>(); 
builder.Services.AddScoped<IWalletQueries, WalletQueries>();       
builder.Services.AddScoped<ISubastaQueries, SubastaQueries>();


// ── APPLICATION: Handlers de los Casos de Uso ──

// Auth
builder.Services.AddScoped<RegisterCommandHandler>();
builder.Services.AddScoped<LoginQueryHandler>();

// Usuarios
builder.Services.AddScoped<GetMisPublicacionesQueryHandler>();
builder.Services.AddScoped<GetMisPujasQueryHandler>();

// Subastas (Auctions)
builder.Services.AddScoped<SearchAuctionsQueryHandler>();
builder.Services.AddScoped<GetAuctionByIdQueryHandler>();
builder.Services.AddScoped<CreateAuctionCommandHandler>();


// ── WORKERS ──
// Encendemos el proceso en segundo plano (Background Worker)
builder.Services.AddHostedService<SubastaYa.WebApi.Workers.AuctionStatusWorker>();

// Wallet
builder.Services.AddScoped<DepositCommandHandler>();
builder.Services.AddScoped<GetBalanceQueryHandler>();
builder.Services.AddScoped<GetTransactionsQueryHandler>();

// Encendemos el proceso en segundo plano (Background Worker)
builder.Services.AddHostedService<SubastaYa.WebApi.Workers.AuctionStatusWorker>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCors(options => {
    options.AddPolicy("PermitirFrontend", policy => {
        policy.WithOrigins("http://localhost:5191", "https://localhost:5191") // ¡Aquí estaba el detalle!
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


//Middlewares




var app = builder.Build();




app.UseMiddleware<ExceptionMiddleware>();




if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("PermitirFrontend");

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
