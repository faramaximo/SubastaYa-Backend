using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auctions.Queries;
using SubastaYa.Domain.Entities;

namespace SubastaYa.Application.UseCases.Auctions.Handlers
{
    public class SearchAuctionsQueryHandler
    {
        private readonly ISubastaQueries _queries;

        public SearchAuctionsQueryHandler(ISubastaQueries queries)
        {
            _queries = queries;
        }

        public async Task<PagedResultDto<AuctionDto>> Handle(SearchAuctionsQuery query)
        {
            return await _queries.SearchAuctionsAsync(
                query.Estado, query.CategoriaId, query.PrecioMin, query.PrecioMax, query.Busqueda, query.OrderBy, query.Page, query.PageSize
            );
        }
    }
}