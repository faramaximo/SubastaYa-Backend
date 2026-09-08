using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Wallet.Commands;
using SubastaYa.Application.UseCases.Wallet.Handlers;
using SubastaYa.Application.UseCases.Wallet.Queries;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")] // Compatibilidad con la interfaz existente.
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

    [HttpGet("balance/{usuarioId:int}")]
    public async Task<IActionResult> GetBalance(int usuarioId)
    {
        var balance = await _balanceHandler.Handle(new GetBalanceQuery(usuarioId));
        return balance is null ? NotFound(new { error = "Billetera no encontrada." }) : Ok(balance);
    }

    [HttpGet("transactions/{usuarioId:int}")]
    public async Task<IActionResult> GetTransactions(int usuarioId) =>
        Ok(await _transactionsHandler.Handle(new GetTransactionsQuery(usuarioId)));

    [HttpPost("deposit/{usuarioId:int}")]
    public async Task<IActionResult> Deposit(int usuarioId, [FromBody] DepositRequestDto dto)
    {
        await _depositHandler.Handle(new DepositCommand(usuarioId, dto.Monto));
        return Ok(new { message = "Depósito realizado correctamente." });
    }
}
