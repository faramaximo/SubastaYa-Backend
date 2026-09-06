// GetMisPublicacionesQueryHandler.cs
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Usuarios.Queries;

namespace SubastaYa.Application.UseCases.Usuarios.Handlers
{
    public class GetMisPublicacionesQueryHandler
    {
        private readonly ISubastaQueries _queries;

        public GetMisPublicacionesQueryHandler(ISubastaQueries queries)
        {
            _queries = queries;
        }

        public async Task<List<PublicacionResumenDto>> Handle(GetMisPublicacionesQuery query)
        {
            return await _queries.GetMisPublicacionesAsync(query.UsuarioId);
        }
    }
}