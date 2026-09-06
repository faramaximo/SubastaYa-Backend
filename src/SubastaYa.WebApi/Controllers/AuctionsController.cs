using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.UseCases.Auctions.Commands;
using SubastaYa.Application.UseCases.Auctions.Queries;
using SubastaYa.Application.UseCases.Auctions.Handlers;
using System.Threading.Tasks;

//su unica responsabilidad es recibir la petición HTTP, delegar el trabajo a _auctionService y retornar un HTTP 200 ( ok).
//Inyección de Dependencias: Al usar IAuctionService en el constructor del controlador, estás respetando el Principio de Inversión de Dependencias (la 'D' de SOLID).

namespace SubastaYa.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController : ControllerBase
    {

        private readonly SearchAuctionsQueryHandler _searchHandler;
        private readonly GetAuctionByIdQueryHandler _getByIdHandler;

        private readonly CreateAuctionCommandHandler _createHandler;
       

        public AuctionsController(
            SearchAuctionsQueryHandler searchHandler,
            GetAuctionByIdQueryHandler getByIdHandler,
            CreateAuctionCommandHandler createHandler)
        {
            _searchHandler = searchHandler;
            _getByIdHandler = getByIdHandler;
            _createHandler = createHandler;
        }

      
        [HttpGet]
        public async Task<IActionResult> GetAuctions(
            [FromQuery] int? estado,
            [FromQuery] int? categoriaId,
            [FromQuery] decimal? precioMin,
            [FromQuery] decimal? precioMax,
            [FromQuery] string? busqueda,
            [FromQuery] string orderBy = "menor-tiempo")
        {
            //El controlador llama al servicio
            var query = new SearchAuctionsQuery(estado, categoriaId, precioMin, precioMax, busqueda, orderBy);
            var subastas = await _searchHandler.Handle(query);
            return Ok(subastas);
        }

        // [HttpGet] con ruta para traer una sola subasta por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuctionById(int id)
        {
            var query = new GetAuctionByIdQuery(id);
            var subasta = await _getByIdHandler.Handle(query);
            if (subasta == null) return NotFound();
            return Ok(subasta);
        }

        

        // [HttpPost] para crear subastas (Módulo 2)
        [HttpPost]
        public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionCommand command)
        {
            var subastaId = await _createHandler.Handle(command);

            // Retorna 201 Created y te dice en qué URL quedó guardada
            return CreatedAtAction(nameof(GetAuctionById), new { id = subastaId }, new { id = subastaId });
        }
    }
}