var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Servir archivos estáticos desde wwwroot
app.UseStaticFiles();

// Redirigir todo a index.html (para SPA o rutas)
app.MapFallbackToFile("index.html");

app.Run();