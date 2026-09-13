using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Auth.Queries;
using SubastaYa.WebApi.Models;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginQueryHandler _loginHandler;
    private readonly RegisterCommandHandler _registerHandler;
    private readonly VerifyEmailCommandHandler _verifyEmailHandler;
    private readonly ResendVerificationCommandHandler _resendVerificationHandler;
    private readonly ForgotPasswordCommandHandler _forgotPasswordHandler;
    private readonly ResetPasswordCommandHandler _resetPasswordHandler;

    public AuthController(
        LoginQueryHandler loginHandler,
        RegisterCommandHandler registerHandler,
        VerifyEmailCommandHandler verifyEmailHandler,
        ResendVerificationCommandHandler resendVerificationHandler,
        ForgotPasswordCommandHandler forgotPasswordHandler,
        ResetPasswordCommandHandler resetPasswordHandler)
    {
        _loginHandler = loginHandler;
        _registerHandler = registerHandler;
        _verifyEmailHandler = verifyEmailHandler;
        _resendVerificationHandler = resendVerificationHandler;
        _forgotPasswordHandler = forgotPasswordHandler;
        _resetPasswordHandler = resetPasswordHandler;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var response = await _loginHandler.Handle(new LoginQuery(dto.Email, dto.Password));
        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await _registerHandler.Handle(new RegisterCommand(dto.Nombre, dto.Email, dto.Password), baseUrl, HttpContext.RequestAborted);
        return Ok(new { mensaje = "Cuenta creada. Revisá tu correo para verificarla." });
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        await _verifyEmailHandler.Handle(new VerifyEmailCommand(token));
        return Ok(new { mensaje = "Correo verificado. Ya podés ingresar." });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] EmailRequestDto dto)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await _resendVerificationHandler.Handle(new ResendVerificationCommand(dto.Email), baseUrl, HttpContext.RequestAborted);
        return Ok(new { mensaje = "Si existe una cuenta pendiente, enviamos un correo de verificación." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] EmailRequestDto dto)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await _forgotPasswordHandler.Handle(new ForgotPasswordCommand(dto.Email, baseUrl), HttpContext.RequestAborted);
        return Ok(new { mensaje = "Si el correo está registrado, recibirás instrucciones para restablecer la contraseña." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _resetPasswordHandler.Handle(new ResetPasswordCommand(dto.Token, dto.Password));
        return Ok(new { mensaje = "Contraseña actualizada. Ya podés ingresar." });
    }
}
