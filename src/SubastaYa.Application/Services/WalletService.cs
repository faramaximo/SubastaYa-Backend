using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly SubastaYaDbContext _context;

        public WalletService(SubastaYaDbContext context)
        {
            _context = context;
        }

        public async Task<WalletBalanceDto?> GetBalanceByUserIdAsync(int usuarioId)
        {
            var wallet = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == usuarioId);

            if (wallet == null) return null;

            return new WalletBalanceDto
            {
                BilleteraId = wallet.Id,
                UsuarioId = wallet.UsuarioId,
                SaldoTotal = wallet.SaldoTotal,
                SaldoRetenido = wallet.SaldoRetenido,
                SaldoDisponible = wallet.SaldoDisponible
            };
        }

        public async Task<bool> DepositAsync(int usuarioId, decimal monto)
        {
            var billetera = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == usuarioId);

            if (billetera == null)
            {
                // Si no existe la billetera, la creamos asignando Version
                billetera = new Billetera
                {
                    UsuarioId = usuarioId,
                    SaldoTotal = monto,
                    SaldoRetenido = 0,
                    Version = Guid.NewGuid() // <- Asignación explícita imprescindible
                };
                await _context.Billeteras.AddAsync(billetera);
            }
            else
            {
                // Si ya existe, incrementamos el saldo y renovamos el token
                billetera.SaldoTotal += monto;
                billetera.Version = Guid.NewGuid(); // <- Asignación explícita al actualizar
            }

            // Registrar en el Ledger
            var transaccion = new TransaccionLedger
            {
                BilleteraId = billetera.Id,
                Monto = monto,
                Tipo = TipoTransaccion.Deposito,
                Fecha = DateTime.UtcNow
            };
            await _context.TransaccionesLedger.AddAsync(transaccion);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TransactionLedgerDto>> GetTransactionsByUserIdAsync(int usuarioId)
        {
            var wallet = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == usuarioId);

            if (wallet == null) return Enumerable.Empty<TransactionLedgerDto>();

            return await _context.TransaccionesLedger
                .Where(t => t.BilleteraId == wallet.Id)
                .OrderByDescending(t => t.Fecha)
                .Select(t => new TransactionLedgerDto
                {
                    Id = t.Id,
                    Tipo = t.Tipo.ToString(),
                    Monto = t.Monto,
                    Fecha = t.Fecha,
                    SubastaId = t.SubastaId
                })
                .ToListAsync();
        }
    }
}
