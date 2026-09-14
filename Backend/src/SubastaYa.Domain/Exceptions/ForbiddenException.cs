namespace SubastaYa.Domain.Exceptions;

/// <summary>Indica que el usuario autenticado no tiene permitido realizar la acción.</summary>
public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message) { }
}
