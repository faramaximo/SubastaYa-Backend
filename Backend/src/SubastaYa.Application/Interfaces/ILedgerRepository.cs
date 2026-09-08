using SubastaYa.Domain.Entities;

namespace SubastaYa.Application.Interfaces
{
    public interface ILedgerRepository
    {
        Task AgregarAsync(TransaccionLedger transaccion);
        // Método en inglés requerido por BidService
        Task AddAsync(TransaccionLedger transaccion);
    }
}