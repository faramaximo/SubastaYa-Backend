using SubastaYa.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

public class Billetera
{
    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public decimal SaldoTotal { get; private set; }
    public decimal SaldoRetenido { get; private set; }
    public decimal SaldoDisponible => SaldoTotal - SaldoRetenido;

    [Timestamp] // Concurrencia optimista visible
    public byte[] Version { get; private set; } = Array.Empty<byte>();

    // Constructor vacío requerido por Entity Framework
    protected Billetera() { }

    // Constructor para cuando creamos una billetera nueva
    public Billetera(int usuarioId)
    {
        UsuarioId = usuarioId;
        SaldoTotal = 0;
        SaldoRetenido = 0;
    }

    // Agregá este constructor abajo del que ya tenías en Billetera.cs
    public Billetera(int usuarioId, decimal saldoTotal, decimal saldoRetenido)
    {
        UsuarioId = usuarioId;
        SaldoTotal = saldoTotal;
        SaldoRetenido = saldoRetenido;
    }


    // El comportamiento vive ADENTRO de la entidad
    public void Depositar(decimal monto)
    {
        if (monto <= 0) throw new DomainException("El monto a depositar debe ser mayor a cero.");

        SaldoTotal += monto;
    }

    public void ProcesarPagoSubasta(decimal monto)
    {
        if (monto <= 0) throw new DomainException("El monto a pagar debe ser mayor a cero.");
        if (SaldoRetenido < monto || SaldoTotal < monto)
            throw new DomainException("La billetera no posee fondos retenidos suficientes para liquidar la subasta.");

        // El pago consume la garantía y debita definitivamente el saldo total.
        SaldoRetenido -= monto;
        SaldoTotal -= monto;
    }
    public void RetenerFondos(decimal monto)
    {
        if (monto <= 0) throw new DomainException("El monto a retener debe ser mayor a cero.");
        if (monto > SaldoDisponible)
            throw new UnprocessableEntityException("Fondos insuficientes para esta puja.");
        SaldoRetenido += monto;
    }

    public void LiberarFondos(decimal monto)
    {
        if (monto <= 0) throw new DomainException("El monto a liberar debe ser mayor a cero.");
        if (monto > SaldoRetenido)
            throw new DomainException("No se puede liberar un monto mayor al saldo retenido.");
        SaldoRetenido -= monto;
    }
}
