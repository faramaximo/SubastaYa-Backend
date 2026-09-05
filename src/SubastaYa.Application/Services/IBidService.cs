using SubastaYa.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Application.Services
{
    public interface IBidService
    {
        Task<PujaResponseDto> RegistrarPujaAsync(RegistrarPujaDto dto);
    }
}
