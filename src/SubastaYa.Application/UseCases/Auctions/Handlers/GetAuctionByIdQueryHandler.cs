using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auctions.Queries;
using SubastaYa.Domain.Entities;

namespace SubastaYa.Application.UseCases.Auctions.Handlers
{
    public class GetAuctionByIdQueryHandler
    {
        private readonly IAuctionRepository _repository;

        public GetAuctionByIdQueryHandler(IAuctionRepository repository)
        {
            _repository = repository;
        }

        public async Task<Subasta?> Handle(GetAuctionByIdQuery query)
        {
            return await _repository.ObtenerPorIdAsync(query.Id);
        }
    }
}