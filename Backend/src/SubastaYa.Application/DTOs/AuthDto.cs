namespace SubastaYa.Application.DTOs;

public record LoginResponseDto(
    int Id,
    string Nombre,
    string Email,
    string Token
);
