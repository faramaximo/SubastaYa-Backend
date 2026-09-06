using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auctions.Commands;
using SubastaYa.Domain.Exceptions;

namespace SubastaYa.Application.UseCases.Auctions.Handlers
{
    public class CreateBidCommandHandler
    {
        private readonly IAuctionRepository _subastas;
        // Si tuvieras que descontar saldo de la billetera, inyectarías IWalletRepository acá.
        private readonly IUnitOfWork _uow;

        public CreateBidCommandHandler(IAuctionRepository subastas, IUnitOfWork uow)
        {
            _subastas = subastas;
            _uow = uow;
        }

        public async Task<int> Handle(CreateBidCommand cmd)
        {
            // 1. Obtener la entidad a través de la abstracción (Repository)
            var subasta = await _subastas.ObtenerPorIdAsync(cmd.SubastaId);

            if (subasta == null)
                throw new DomainException($"No se encontró la subasta con ID {cmd.SubastaId}");

            // 2. Delegar la lógica a la Entidad Rica
            // Acá NO HAY ifs preguntando si la subasta está activa o si el monto alcanza.
            // La entidad Subasta hace todas esas validaciones internamente.
            subasta.RegistrarPuja(cmd.CompradorId, cmd.Monto);

            // 3. Confirmar la transacción completa
            // Si tuvieras _billetera.DescontarSaldo(monto) arriba, el SaveChangesAsync
            // garantiza que O se guarda la puja Y el descuento, O no se guarda NADA (Atomicidad).
            await _uow.SaveChangesAsync();

            return subasta.Id;
        }
    }
}