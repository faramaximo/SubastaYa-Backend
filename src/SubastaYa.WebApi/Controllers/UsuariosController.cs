using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.UseCases.Usuarios.Handlers;
using SubastaYa.Application.UseCases.Usuarios.Queries;

namespace SubastaYa.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly GetMisPublicacionesQueryHandler _publicacionesHandler;
        private readonly GetMisPujasQueryHandler _pujasHandler;

        public UsuariosController(
            GetMisPublicacionesQueryHandler publicacionesHandler,
            GetMisPujasQueryHandler pujasHandler)
        {
            _publicacionesHandler = publicacionesHandler;
            _pujasHandler = pujasHandler;
        }

        [HttpGet("{id}/publicaciones")]
        public async Task<IActionResult> GetMisPublicaciones(int id)
        {
            var result = await _publicacionesHandler.Handle(new GetMisPublicacionesQuery(id));
            return Ok(result);
        }

        [HttpGet("{id}/pujas")]
        public async Task<IActionResult> GetMisPujas(int id)
        {
            var result = await _pujasHandler.Handle(new GetMisPujasQuery(id));
            return Ok(result);
        }
    }
}