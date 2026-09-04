using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.DTOs;
using SubastaYa.Domain.Entities;
using SubastaYa.Infrastructure.Data;


namespace SubastaYa.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SubastaYaDbContext _context;

        public AuthController(SubastaYaDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // 1. Buscamos al usuario por su email
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            // 2. Verificamos que exista y que la contraseña coincida con el Hash (Usando BCrypt)
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
            {
                return Unauthorized(new { mensaje = "Email o contraseña incorrectos." });
            }

            // 3. Devolvemos los datos del usuario (sin la contraseña por seguridad)
            return Ok(new
            {
                id = usuario.Id,
                nombre = usuario.Nombre,
                email = usuario.Email
            });
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // 1. Verificamos que el email no esté en uso
            var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email == dto.Email);
            if (emailExiste)
            {
                // Devolvemos 409 Conflict siguiendo las reglas RESTful de tu TP
                return Conflict(new { mensaje = "Este correo electrónico ya está registrado." });
            }

            // 2. Creamos el usuario encriptando su clave
            var nuevoUsuario = new Usuario
            {
                Nombre = dto.Nombre,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(nuevoUsuario);

            // Guardamos para que MySQL/SQLite le asigne un ID (lo necesitamos para la billetera)
            await _context.SaveChangesAsync();

            // 3. REGLA DE NEGOCIO: Le creamos su billetera en cero
            var nuevaBilletera = new Billetera
            {
                UsuarioId = nuevoUsuario.Id,
                SaldoTotal = 0,
                SaldoRetenido = 0
            };

            _context.Billeteras.Add(nuevaBilletera);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Cuenta creada con éxito." });
        }




    }
}