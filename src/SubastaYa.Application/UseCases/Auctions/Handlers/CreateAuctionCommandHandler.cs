using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auctions.Commands;
using SubastaYa.Domain.Entities;

namespace SubastaYa.Application.UseCases.Auctions.Handlers
{
    public class CreateAuctionCommandHandler
    {
        private readonly IAuctionRepository _repository;
        private readonly IUnitOfWork _uow;

        public CreateAuctionCommandHandler(IAuctionRepository repository, IUnitOfWork uow)
        {
            _repository = repository;
            _uow = uow;
        }

        public async Task<int> Handle(CreateAuctionCommand cmd)
        {
            // Usamos la entidad rica que armamos antes
            var subasta = new Subasta(
                cmd.VendedorId,
                cmd.CategoriaId,
                cmd.Titulo,
                cmd.Descripcion,
                cmd.UrlImagen,
                cmd.PrecioBase,
                cmd.IncrementoMinimo,
                cmd.FechaFin
            );

            await _repository.AgregarAsync(subasta);
            await _uow.SaveChangesAsync(); // Unit of Work confirma la transacción

            return subasta.Id;
        }
    }
}