using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auctions.Queries;
using SubastaYa.Application.DTOs;
using SubastaYa.Domain.Entities;
using System.Linq;

namespace SubastaYa.Application.UseCases.Auctions.Handlers
{
    public class GetAuctionByIdQueryHandler
    {
        private readonly IAuctionRepository _repository;

        public GetAuctionByIdQueryHandler(IAuctionRepository repository)
        {
            _repository = repository;
        }

        public async Task<AuctionDto?> Handle(GetAuctionByIdQuery query)
        {
            var subasta = await _repository.ObtenerPorIdAsync(query.Id);
            if (subasta is null) return null;

            var dto = new AuctionDto
            {
                Id = subasta.Id,
                Titulo = subasta.Titulo,
                Descripcion = subasta.Descripcion,
                UrlImagen = subasta.UrlImagen,
                CategoriaNombre = subasta.Categoria?.Nombre ?? string.Empty,
                PrecioBase = subasta.PrecioBase,
                IncrementoMinimo = subasta.IncrementoMinimo,
                OfertaMasAlta = subasta.Pujas.Any() ? subasta.Pujas.Max(p => p.Monto) : 0,
                CantidadOfertas = subasta.Pujas.Count,
                FechaInicio = subasta.FechaInicio,
                FechaFin = subasta.FechaFin,
                Estado = subasta.Estado,
                Pujas = subasta.Pujas.Select(p => new PujaInfoDto(p.Id, p.CompradorId, p.Monto, p.FechaPuja)).ToList()
            };

            return dto;
        }
    }
}
