namespace SubastaYa.Application.UseCases.Bids.Commands;

public record RegisterBidCommand(int SubastaId, int UsuarioId, decimal Monto);
