using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Auth.Handlers; // <-- Acá encuentra a RegisterCommandHandler y LoginQueryHandler
using SubastaYa.Application.UseCases.Auth.Commands; // <-- Acá encuentra a RegisterCommand (si lo pusiste en su propia carpeta)
using SubastaYa.Application.UseCases.Auth.Queries;


namespace SubastaYa.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RegisterCommandHandler _registerHandler;
        private readonly LoginQueryHandler _loginHandler;

        public AuthController(RegisterCommandHandler registerHandler, LoginQueryHandler loginHandler)
        {
            _registerHandler = registerHandler;
            _loginHandler = loginHandler;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _loginHandler.Handle(new LoginQuery(dto.Email, dto.Password));
            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            await _registerHandler.Handle(new RegisterCommand(dto.Nombre, dto.Email, dto.Password));
            return Ok(new { mensaje = "Cuenta creada con éxito." });
        }
    }
}