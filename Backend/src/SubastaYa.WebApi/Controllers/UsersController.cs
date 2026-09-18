using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Usuarios.Handlers;
using SubastaYa.Application.UseCases.Usuarios.Queries;
using SubastaYa.WebApi.Models;
using System.Security.Claims;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly RegisterUserCommandHandler _registerHandler;
    private readonly GetMisPublicacionesQueryHandler _publicacionesHandler;
    private readonly GetMisPujasQueryHandler _pujasHandler;
    private readonly VerifyEmailCommandHandler _verifyEmailHandler;
    private readonly ResendEmailVerificationCommandHandler _resendEmailVerificationHandler;

    public UsersController(
        RegisterUserCommandHandler registerHandler,
        GetMisPublicacionesQueryHandler publicacionesHandler,
        GetMisPujasQueryHandler pujasHandler,
        VerifyEmailCommandHandler verifyEmailHandler,
        ResendEmailVerificationCommandHandler resendEmailVerificationHandler)
    {
        _registerHandler = registerHandler;
        _publicacionesHandler = publicacionesHandler;
        _pujasHandler = pujasHandler;
        _verifyEmailHandler = verifyEmailHandler;
        _resendEmailVerificationHandler = resendEmailVerificationHandler;
    }

    /// <summary>
    /// POST /api/v1/users (Reemplaza POST /api/Auth/register)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] RegisterDto dto, CancellationToken cancellationToken)
    {
        var origin = $"{Request.Scheme}://{Request.Host}";
        await _registerHandler.Handle(new RegisterUserCommand(dto.Nombre, dto.Email, dto.Password, origin), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { mensaje = "Cuenta creada. Revisá tu correo para verificarla." });
    }

    /// <summary>
    /// GET /api/v1/users/email-verifications?token=xxx (Soporta clic directo desde el correo)
    /// </summary>
    [HttpGet("email-verifications")]
    public async Task<IActionResult> VerifyEmailByGet([FromQuery] string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { error = "El token de verificación es obligatorio." });

        await _verifyEmailHandler.Handle(new VerifyEmailCommand(token), cancellationToken);
        return Ok(new { mensaje = "Correo verificado. Ya podés ingresar." });
    }

    /// <summary>
    /// POST /api/v1/users/email-verifications (Verificación vía payload { token } o reenvío con { email })
    /// </summary>
    [HttpPost("email-verifications")]
    public async Task<IActionResult> ProcessEmailVerification([FromBody] EmailVerificationRequestDto dto, CancellationToken cancellationToken)
    {
        // 1. Confirmar token mediante POST
        if (!string.IsNullOrWhiteSpace(dto.Token))
        {
            await _verifyEmailHandler.Handle(new VerifyEmailCommand(dto.Token), cancellationToken);
            return Ok(new { mensaje = "Correo verificado. Ya podés ingresar." });
        }

        // 2. Reenviar enlace de verificación mediante POST
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            await _resendEmailVerificationHandler.Handle(
                new ResendEmailVerificationCommand(dto.Email, $"{Request.Scheme}://{Request.Host}"), cancellationToken);
            return Ok(new { mensaje = "Si existe una cuenta pendiente, enviamos un correo de verificación." });
        }

        return BadRequest(new { error = "Debe proporcionar un token de verificación o un email." });
    }

    [Authorize]
    [HttpGet("auctions")]
    [HttpGet("{userId:int}/auctions")]
    public async Task<IActionResult> GetMisPublicaciones(int? userId = null)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (userId.HasValue && userId.Value != usuarioId)
            return Forbid();

        return Ok(await _publicacionesHandler.Handle(new GetMisPublicacionesQuery(usuarioId)));
    }

    [Authorize]
    [HttpGet("bids")]
    [HttpGet("{userId:int}/bids")]
    public async Task<IActionResult> GetMisPujas(int? userId = null)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (userId.HasValue && userId.Value != usuarioId)
            return Forbid();

        return Ok(await _pujasHandler.Handle(new GetMisPujasQuery(usuarioId)));
    }
}

public record EmailVerificationRequestDto(string? Token = null, string? Email = null);
