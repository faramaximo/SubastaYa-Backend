using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.UseCases.Usuarios.Handlers;
using SubastaYa.Application.UseCases.Usuarios.Queries;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpGet("{id:int}/publicaciones")]
    public async Task<IActionResult> GetMisPublicaciones(int id) =>
        Ok(await _publicacionesHandler.Handle(new GetMisPublicacionesQuery(id)));

    [HttpGet("{id:int}/pujas")]
    public async Task<IActionResult> GetMisPujas(int id) =>
        Ok(await _pujasHandler.Handle(new GetMisPujasQuery(id)));
}
