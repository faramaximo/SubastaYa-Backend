using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auctions.Handlers;
using SubastaYa.Application.UseCases.Auctions.Commands;
using SubastaYa.Application.UseCases.Bids.Commands;
using SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Usuarios.Handlers;
using SubastaYa.Application.UseCases.Wallet.Handlers;
using SubastaYa.Infrastructure.Data;
using SubastaYa.Infrastructure.Persistence.Queries;
using SubastaYa.Infrastructure.Persistence.Repositories;
using SubastaYa.Infrastructure.Repositories;
using SubastaYa.WebApi.Hubs;
using SubastaYa.WebApi.Middlewares;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SubastaYa.Infrastructure.Services;
using SubastaYa.WebApi.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Cadena de conexión e Infraestructura
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<SubastaYaDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Factory para crear instancias independientes del DbContext (usado por AuditService
// para persistir auditorías fuera de la transacción principal del handler).
builder.Services.AddDbContextFactory<SubastaYaDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
    ServiceLifetime.Scoped);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Evitar ciclos de referencia al serializar entidades de EF Core
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Configuración de autenticación JWT
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? string.Empty;
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true
        };
    });
}

// 2. Registros de Infraestructura
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ILedgerRepository, LedgerRepository>();
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<IWalletQueries, WalletQueries>();
builder.Services.AddScoped<ISubastaQueries, SubastaQueries>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuctionNotifier, SignalRAuctionNotifier>(); // ← NUEVA LÍNEA

// 3. Registros de Aplicación (Auth, Subastas, Billetera y Pujas)
builder.Services.AddScoped<RegisterUserCommandHandler>();
builder.Services.AddScoped<LoginQueryHandler>();
builder.Services.AddScoped<ForgotPasswordCommandHandler>();
builder.Services.AddScoped<ResetPasswordCommandHandler>();
builder.Services.AddScoped<VerifyEmailCommandHandler>();
builder.Services.AddScoped<ResendEmailVerificationCommandHandler>();
builder.Services.AddScoped<GetMisPublicacionesQueryHandler>();
builder.Services.AddScoped<GetMisPujasQueryHandler>();
builder.Services.AddScoped<SearchAuctionsQueryHandler>();
builder.Services.AddScoped<GetAuctionByIdQueryHandler>();
builder.Services.AddScoped<CreateAuctionCommandHandler>();
builder.Services.AddScoped<FinalizeAuctionCommandHandler>();
builder.Services.AddScoped<StartScheduledAuctionsCommandHandler>();
builder.Services.AddScoped<RegisterBidCommandHandler>();

builder.Services.AddScoped<DepositCommandHandler>();
builder.Services.AddScoped<GetBalanceQueryHandler>();
builder.Services.AddScoped<GetTransactionsQueryHandler>();

// 4. Worker en Segundo Plano y SignalR
builder.Services.AddHostedService<SubastaYa.WebApi.Workers.AuctionStatusWorker>();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SubastaYa API",
        Version = "v1",
        Description = "API de SubastaYa con soporte para autenticación JWT Bearer."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Autenticación JWT Bearer. Ingrese 'Bearer' seguido de un espacio y su token. Ejemplo: \"Bearer eyJhbGciOi...\"",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configuración de CORS incluyendo los puertos de desarrollo local
builder.Services.AddCors(options => {
    options.AddPolicy("PermitirFrontend", policy => {
        policy.WithOrigins(
                    "http://localhost:5216", "https://localhost:5216",
                    "http://localhost:5191", "https://localhost:5191",
                    "http://localhost:5173", "https://localhost:5173"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ── INTERFAZ WEB ──
// La API hospeda la interfaz real del proyecto. No se usa el template Vite/Aspire
// que quedó en Fronted/frontend luego de separar el monorepo.
var interfazPath = Path.GetFullPath(Path.Combine(
    builder.Environment.ContentRootPath,
    "..", "..", "..", "Fronted", "SubastaYa-Fronted", "wwwroot"));

if (!Directory.Exists(interfazPath))
{
    var contenedorWwwroot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
    if (Directory.Exists(contenedorWwwroot))
    {
        interfazPath = contenedorWwwroot;
    }
}

PhysicalFileProvider? interfazFileProvider = null;

if (Directory.Exists(interfazPath))
{
    interfazFileProvider = new PhysicalFileProvider(interfazPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = interfazFileProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = interfazFileProvider });
}
else
{
    app.Logger.LogWarning("No se encontró la interfaz web en {RutaInterfaz}", interfazPath);
}

// IMPORTANT: ordenar middlewares de routing y seguridad correctamente
app.UseRouting();

app.UseCors("PermitirFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AuctionHub>("/hubs/auction");

// Permite recargar o navegar a rutas de la interfaz sin interceptar API ni SignalR.
if (interfazFileProvider is not null)
{
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = interfazFileProvider
    });
}

// Inicialización de la base de datos (Migraciones + Seeder)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SubastaYaDbContext>();
        await context.Database.MigrateAsync();
        await SubastaYa.Infrastructure.Seed.DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al aplicar migraciones o inicializar datos semilla.");
    }
}

app.Run();
