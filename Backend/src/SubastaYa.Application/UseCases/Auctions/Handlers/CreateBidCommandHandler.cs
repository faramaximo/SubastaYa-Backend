using Microsoft.EntityFrameworkCore; // <-- Necesitas esto para atrapar el error de EF Core
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auctions.Commands;
using SubastaYa.Domain.Exceptions;

namespace SubastaYa.Application.UseCases.Auctions.Handlers
{
    public class CreateBidCommandHandler
    {
        private readonly IAuctionRepository _subastas;
        private readonly IUnitOfWork _uow;

        public CreateBidCommandHandler(IAuctionRepository subastas, IUnitOfWork uow)
        {
            _subastas = subastas;
            _uow = uow;
        }

        public async Task<int> Handle(CreateBidCommand cmd)
        {
            var subasta = await _subastas.ObtenerPorIdAsync(cmd.SubastaId);

            if (subasta == null)
                throw new DomainException($"No se encontró la subasta con ID {cmd.SubastaId}");

            // El dominio aplica las reglas de negocio
            subasta.RegistrarPuja(cmd.CompradorId, cmd.Monto);

            try
            {
                // Intentamos confirmar la transacción
                await _uow.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // El Handler traduce el error técnico de infraestructura a un error de negocio.
                // Tu ExceptionMiddleware ya está configurado para atrapar esto y devolver un 400.
                throw new ConcurrencyException("Alguien más realizó una puja al mismo tiempo. Por favor, actualiza la subasta y vuelve a intentarlo.");
            }

            return subasta.Id;
        }
    }
}