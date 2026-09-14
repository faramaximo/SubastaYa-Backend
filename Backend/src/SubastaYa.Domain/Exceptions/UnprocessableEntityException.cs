namespace SubastaYa.Domain.Exceptions;

/// <summary>Indica que la petición es válida pero viola una regla de negocio.</summary>
public sealed class UnprocessableEntityException : DomainException
{
    public UnprocessableEntityException(string message) : base(message) { }
}
