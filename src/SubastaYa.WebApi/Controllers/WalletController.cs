using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;

namespace SubastaYa.WebApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("balance/{usuarioId}")]
        public async Task<IActionResult> GetBalance(int usuarioId)
        {
            var balance = await _walletService.GetBalanceByUserIdAsync(usuarioId);
            if (balance == null) return NotFound("Billetera no encontrada.");
            return Ok(balance);
        }

        [HttpPost("deposit/{usuarioId}")]
        public async Task<IActionResult> Deposit(int usuarioId, [FromBody] DepositRequestDto dto)
        {
            var success = await _walletService.DepositAsync(usuarioId, dto.Monto);
            if (!success) return BadRequest("Monto inválido o usuario no encontrado.");
            return Ok(new { message = "Depósito realizado correctamente." });
        }

        [HttpGet("transactions/{usuarioId}")]
        public async Task<IActionResult> GetTransactions(int usuarioId)
        {
            var transactions = await _walletService.GetTransactionsByUserIdAsync(usuarioId);
            return Ok(transactions);
        }
    }
}
