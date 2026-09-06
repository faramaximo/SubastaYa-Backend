var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Servir archivos estáticos desde wwwroot (o desde la raíz)
app.UseStaticFiles();

// Para que cualquier ruta no encontrada vaya a index.html (SPA)
app.MapFallbackToFile("index.html");

app.Run();