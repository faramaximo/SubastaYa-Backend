using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Application.UseCases.Usuarios.Queries
{
    public record GetMisPublicacionesQuery(int UsuarioId);

    public record GetMisPujasQuery(int UsuarioId);
}