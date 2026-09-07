using SubastaYa.Domain.Exceptions;

public class Billetera
{
    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public decimal SaldoTotal { get; private set; }
    public decimal SaldoRetenido { get; private set; }
    public decimal SaldoDisponible => SaldoTotal - SaldoRetenido;
    public Guid Version { get; private set; }

    // Constructor vacío requerido por Entity Framework
    protected Billetera() { }

    // Constructor para cuando creamos una billetera nueva
    public Billetera(int usuarioId)
    {
        UsuarioId = usuarioId;
        SaldoTotal = 0;
        SaldoRetenido = 0;
        Version = Guid.NewGuid();
    }

    // Agregá este constructor abajo del que ya tenías en Billetera.cs
    public Billetera(int usuarioId, decimal saldoTotal, decimal saldoRetenido)
    {
        UsuarioId = usuarioId;
        SaldoTotal = saldoTotal;
        SaldoRetenido = saldoRetenido;
        Version = Guid.NewGuid();
    }


    // El comportamiento vive ADENTRO de la entidad
    public void Depositar(decimal monto)
    {
        if (monto <= 0) throw new DomainException("El monto a depositar debe ser mayor a cero.");

        SaldoTotal += monto;
        Version = Guid.NewGuid(); // La entidad controla su propia versión
    }

    public void ProcesarPagoSubasta(decimal monto)
    {
        if (monto <= 0) throw new DomainException("El monto a pagar debe ser mayor a cero.");

        // La entidad se encarga de sus propias matemáticas
        SaldoRetenido -= monto;
        SaldoTotal -= monto;
        Version = Guid.NewGuid();
    }
    public void RetenerFondos(decimal monto)
    {
        if (monto > SaldoDisponible)
            throw new DomainException("Fondos insuficientes para esta puja.");
        SaldoRetenido += monto;
        Version = Guid.NewGuid();
    }

    public void LiberarFondos(decimal monto)
    {
        SaldoRetenido -= monto;
        Version = Guid.NewGuid();
    }
}