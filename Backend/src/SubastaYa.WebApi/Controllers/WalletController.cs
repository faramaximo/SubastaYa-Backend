using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Wallet.Commands;
using SubastaYa.Application.UseCases.Wallet.Handlers;
using SubastaYa.Application.UseCases.Wallet.Queries;

namespace SubastaYa.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/wallet")]
public class WalletController : ControllerBase
{
    private readonly DepositCommandHandler _depositHandler;
    private readonly GetBalanceQueryHandler _balanceHandler;
    private readonly GetTransactionsQueryHandler _transactionsHandler;

    public WalletController(
        DepositCommandHandler depositHandler,
        GetBalanceQueryHandler balanceHandler,
        GetTransactionsQueryHandler transactionsHandler)
    {
        _depositHandler = depositHandler;
        _balanceHandler = balanceHandler;
        _transactionsHandler = transactionsHandler;
    }

    /// <summary>
    /// GET /api/v1/wallet
    /// Devuelve el balance y estado de la billetera del usuario.
    /// </summary>
    [HttpGet]
    [HttpGet("~/api/v1/users/{usuarioId:int}/wallet")]
    public async Task<IActionResult> GetBalance([FromRoute] int? usuarioId = null)
    {
        var authUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (usuarioId.HasValue && authUserId != usuarioId.Value)
            return Forbid();

        var balance = await _balanceHandler.Handle(new GetBalanceQuery(authUserId));
        return balance is null ? NotFound(new { error = "Billetera no encontrada." }) : Ok(balance);
    }

    /// <summary>
    /// GET /api/v1/wallet/transactions
    /// Devuelve el historial de transacciones de la billetera (Ledger).
    /// </summary>
    [HttpGet("transactions")]
    [HttpGet("~/api/v1/users/{usuarioId:int}/wallet/transactions")]
    public async Task<IActionResult> GetTransactions([FromRoute] int? usuarioId = null)
    {
        var authUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (usuarioId.HasValue && authUserId != usuarioId.Value)
            return Forbid();

        return Ok(await _transactionsHandler.Handle(new GetTransactionsQuery(authUserId)));
    }

    /// <summary>
    /// POST /api/v1/wallet/deposits
    /// Procesa la creación de un nuevo depósito en la billetera.
    /// </summary>
    [HttpPost("deposits")]
    [HttpPost("~/api/v1/users/{usuarioId:int}/wallet/deposits")]
    public async Task<IActionResult> Deposit([FromBody] DepositRequestDto dto, [FromRoute] int? usuarioId = null)
    {
        var authUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (usuarioId.HasValue && authUserId != usuarioId.Value)
            return Forbid();

        await _depositHandler.Handle(new DepositCommand(authUserId, dto.Monto));
        return Ok(new { message = "Depósito realizado correctamente." });
    }
}
