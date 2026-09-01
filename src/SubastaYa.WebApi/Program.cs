using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.Services;
using SubastaYa.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Configurar la conexión a la base de datos de manera Code-First
builder.Services.AddDbContext<SubastaYaDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Registrar dependencias de la aplicacion
builder.Services.AddScoped<IAuctionService, AuctionService>();

// Configure Add controllers para la API
builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

app.Run();
