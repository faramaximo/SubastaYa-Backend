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


    public async Task<IEnumerable<AuctionDto>> ObtenerSubastasAsync(int? estado, int? categoriaId, decimal? precioMin, decimal? precioMax, string? busqueda, string orderBy)
    {
        // Agregamos Include(s => s.Pujas) para poder saber cuál es la oferta real más alta
        var query = _context.Subastas
            .Include(s => s.Categoria)
            .Include(s => s.Pujas)
            .AsQueryable();

        // Filtro por Estado (Corregido: convertimos el número que llega de la web al Enum)
        // Filtro por Estado: Si el usuario pide Finalizadas (2), le mandamos las Finalizadas (2) Y las Desiertas (3)
        if (estado.HasValue)
        {
            if (estado.Value == 2)
            {
                // Trae ambas porque ambas están terminadas
                query = query.Where(s => s.Estado == EstadoSubasta.Finalizada || s.Estado == EstadoSubasta.Desierta);
            }
            else
            {
                // Para Activas (1) o Próximas (0), filtra normal
                query = query.Where(s => s.Estado == (EstadoSubasta)estado.Value);
            }
        }

        // Filtro por Categoría
        if (categoriaId.HasValue)
            query = query.Where(s => s.CategoriaId == categoriaId.Value);

        // Filtro por Búsqueda (Título)
        if (!string.IsNullOrEmpty(busqueda))
            query = query.Where(s => s.Titulo.Contains(busqueda));

        // Filtro por Precio (Calculando dinámicamente si tiene pujas o usamos el precio base)
        if (precioMin.HasValue)
            query = query.Where(s => (s.Pujas.Any() ? s.Pujas.Max(p => p.Monto) : s.PrecioBase) >= precioMin.Value);

        if (precioMax.HasValue)
            query = query.Where(s => (s.Pujas.Any() ? s.Pujas.Max(p => p.Monto) : s.PrecioBase) <= precioMax.Value);

        // Ordenamiento desde la Base de Datos
        switch (orderBy)
        {
            case "mayor-tiempo":
                query = query.OrderByDescending(s => s.FechaFin);
                break;
            case "menor-puja":
                query = query.OrderBy(s => s.Pujas.Any() ? s.Pujas.Max(p => p.Monto) : s.PrecioBase);
                break;
            case "mayor-puja":
                query = query.OrderByDescending(s => s.Pujas.Any() ? s.Pujas.Max(p => p.Monto) : s.PrecioBase);
                break;
            case "menor-tiempo":
            default:
                query = query.OrderBy(s => s.FechaFin);
                break;
        }

        var subastas = await query.ToListAsync();

        // Mapear a tu DTO (Corregido: quitamos el (int) de Estado)
        return subastas.Select(s => new AuctionDto
        {
            Id = s.Id,
            Titulo = s.Titulo,
            CategoriaNombre = s.Categoria?.Nombre ?? "",
            UrlImagen = s.UrlImagen,
            OfertaMasAlta = s.Pujas.Any() ? s.Pujas.Max(p => p.Monto) : s.PrecioBase, // Valor real de la puja actual


            FechaInicio = s.FechaInicio, // 👈 ¡ESTA ES LA LÍNEA MÁGICA QUE FALTABA!
            FechaFin = s.FechaFin,
            CantidadOfertas = s.Pujas.Count,
            Estado = s.Estado
        });
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
            Estado = EstadoSubasta.Programada // Valor inicial por defecto
        };

        _context.Subastas.Add(subasta);
        await _context.SaveChangesAsync();

        return await GetAuctionByIdAsync(subasta.Id) ?? throw new Exception("Error al recuperar la subasta tras la creación");
    }
}
