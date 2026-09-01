using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.Application.Services;

public class AuctionService : IAuctionService
{
    private readonly SubastaYaDbContext _context;

    public AuctionService(SubastaYaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuctionDto>> GetAllAuctionsAsync(EstadoSubasta? estado, int? categoriaId)
    {
        var query = _context.Subastas
            .Include(s => s.Categoria)
            .Include(s => s.Pujas)
            .AsQueryable();

        if (estado.HasValue)
        {
            query = query.Where(s => s.Estado == estado.Value);
        }

        if (categoriaId.HasValue)
        {
            query = query.Where(s => s.CategoriaId == categoriaId.Value);
        }

        var subastas = await query.ToListAsync();

        return subastas.Select(s => new AuctionDto
        {
            Id = s.Id,
            Titulo = s.Titulo,
            Descripcion = s.Descripcion,
            UrlImagen = s.UrlImagen,
            CategoriaNombre = s.Categoria?.Nombre ?? "",
            PrecioBase = s.PrecioBase,
            IncrementoMinimo = s.IncrementoMinimo,
            Estado = s.Estado,
            FechaInicio = s.FechaInicio,
            FechaFin = s.FechaFin,
            CantidadOfertas = s.Pujas.Count,
            OfertaMasAlta = s.Pujas.Any() ? s.Pujas.Max(p => p.Monto) : s.PrecioBase
        });
    }

    public async Task<AuctionDto?> GetAuctionByIdAsync(int id)
    {
        var subasta = await _context.Subastas
            .Include(s => s.Categoria)
            .Include(s => s.Pujas)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subasta == null) return null;

        return new AuctionDto
        {
            Id = subasta.Id,
            Titulo = subasta.Titulo,
            Descripcion = subasta.Descripcion,
            UrlImagen = subasta.UrlImagen,
            CategoriaNombre = subasta.Categoria?.Nombre ?? "",
            PrecioBase = subasta.PrecioBase,
            IncrementoMinimo = subasta.IncrementoMinimo,
            Estado = subasta.Estado,
            FechaInicio = subasta.FechaInicio,
            FechaFin = subasta.FechaFin,
            CantidadOfertas = subasta.Pujas.Count,
            OfertaMasAlta = subasta.Pujas.Any() ? subasta.Pujas.Max(p => p.Monto) : subasta.PrecioBase
        };
    }

    public async Task<AuctionDto> CreateAuctionAsync(CreateAuctionDto createAuctionDto)
    {
        var subasta = new Subasta
        {
            Titulo = createAuctionDto.Titulo,
            Descripcion = createAuctionDto.Descripcion,
            UrlImagen = createAuctionDto.UrlImagen,
            CategoriaId = createAuctionDto.CategoriaId,
            VendedorId = createAuctionDto.VendedorId,
            PrecioBase = createAuctionDto.PrecioBase,
            IncrementoMinimo = createAuctionDto.IncrementoMinimo,
            FechaInicio = createAuctionDto.FechaInicio,
            FechaFin = createAuctionDto.FechaFin,
            Estado = EstadoSubasta.Proxima // Valor inicial por defecto
        };

        _context.Subastas.Add(subasta);
        await _context.SaveChangesAsync();

        return await GetAuctionByIdAsync(subasta.Id) ?? throw new Exception("Error al recuperar la subasta tras la creación");
    }
}
