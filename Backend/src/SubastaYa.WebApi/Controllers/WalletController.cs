using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Wallet.Commands;
using SubastaYa.Application.UseCases.Wallet.Handlers;
using SubastaYa.Application.UseCases.Wallet.Queries;

namespace SubastaYa.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/wallets")]
public class WalletController : ControllerBase
{
    private readonly DepositCommandHandler _depositHandler;
    private readonly GetBalanceQueryHandler _balanceHandler;
    private readonly GetTransactionsQueryHandler _transactionsHandler;

    public WalletController(DepositCommandHandler depositHandler, GetBalanceQueryHandler balanceHandler, GetTransactionsQueryHandler transactionsHandler)
    {
        _depositHandler = depositHandler;
        _balanceHandler = balanceHandler;
        _transactionsHandler = transactionsHandler;
    }

    [HttpGet("balance")]
    [ProducesResponseType(typeof(WalletBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance()
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var balance = await _balanceHandler.Handle(new GetBalanceQuery(usuarioId));
        return balance is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Recurso no encontrado", detail: "La billetera especificada no existe.")
            : Ok(balance);
    }

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactions()
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var balance = await _balanceHandler.Handle(new GetBalanceQuery(usuarioId));
        if (balance is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Recurso no encontrado", detail: "La billetera especificada no existe.");

        return Ok(await _transactionsHandler.Handle(new GetTransactionsQuery(usuarioId)));
    }

    [HttpPost("deposits")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateDeposit([FromBody] CreateDepositRequestDto dto)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _depositHandler.Handle(new DepositCommand(usuarioId, dto.Monto!.Value));
        return Created("/api/v1/wallets/transactions", new { mensaje = "Depósito realizado correctamente." });
    }
}
public sealed class CreateDepositRequestDto
{
    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Range(typeof(decimal), "1", "10000000", ErrorMessage = "El monto debe ser entre $1 y $10.000.000.")]
    public decimal? Monto { get; init; }
}