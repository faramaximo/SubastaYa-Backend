using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Usuarios.Queries;

namespace SubastaYa.Application.UseCases.Usuarios.Handlers
{
    public class GetMisPujasQueryHandler
    {
        private readonly ISubastaQueries _queries;

        public GetMisPujasQueryHandler(ISubastaQueries queries)
        {
            _queries = queries;
        }

        public async Task<List<ParticipacionResumenDto>> Handle(GetMisPujasQuery query)
        {
            return await _queries.GetMisPujasAsync(query.UsuarioId);
        }
    }
}