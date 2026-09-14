using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Auth.Queries;
using SubastaYa.WebApi.Models;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly LoginQueryHandler _loginHandler;
    private readonly ForgotPasswordCommandHandler _forgotPasswordHandler;
    private readonly ResetPasswordCommandHandler _resetPasswordHandler;

    public AuthController(
        LoginQueryHandler loginHandler,
        ForgotPasswordCommandHandler forgotPasswordHandler,
        ResetPasswordCommandHandler resetPasswordHandler)
    {
        _loginHandler = loginHandler;
        _forgotPasswordHandler = forgotPasswordHandler;
        _resetPasswordHandler = resetPasswordHandler;
    }

    /// <summary>
    /// POST /api/v1/auth/tokens (Reemplaza POST /api/Auth/login)
    /// </summary>
    [HttpPost("tokens")]
    public async Task<IActionResult> CreateToken([FromBody] LoginDto dto)
    {
        return Ok(await _loginHandler.Handle(new LoginQuery(dto.Email, dto.Password)));
    }

    /// <summary>
    /// POST /api/v1/auth/password-resets (Reemplaza POST /api/Auth/forgot-password)
    /// </summary>
    [HttpPost("password-resets")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] EmailRequestDto dto, CancellationToken cancellationToken)
    {
        await _forgotPasswordHandler.Handle(new ForgotPasswordCommand(dto.Email, $"{Request.Scheme}://{Request.Host}"), cancellationToken);
        return Ok(new { mensaje = "Si el correo está registrado, recibirás instrucciones para restablecer la contraseña." });
    }

    /// <summary>
    /// POST /api/v1/auth/password-resets/confirm
    /// </summary>
    [HttpPost("password-resets/confirm")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        await _resetPasswordHandler.Handle(new ResetPasswordCommand(dto.Token, dto.Password), cancellationToken);
        return Ok(new { mensaje = "Contraseña actualizada. Ya podés ingresar." });
    }
}
