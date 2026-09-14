namespace SubastaYa.Domain.Exceptions;

/// <summary>Indica que el recurso solicitado no existe.</summary>
public sealed class ResourceNotFoundException : DomainException
{
    public ResourceNotFoundException(string message) : base(message) { }
}
