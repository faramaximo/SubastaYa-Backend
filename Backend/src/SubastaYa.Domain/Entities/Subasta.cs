using System.ComponentModel.DataAnnotations; // <-- Necesitas agregar este using
using SubastaYa.Domain.Enums;
using SubastaYa.Domain.Exceptions;

namespace SubastaYa.Domain.Entities
{
    public class Subasta
    {   
        public int Id { get; private set; }
        public int VendedorId { get; private set; }
        public int CategoriaId { get; private set; }
        public string Titulo { get; private set; }
        public string Descripcion { get; private set; }
        public string UrlImagen { get; private set; }
        public decimal PrecioBase { get; private set; }
        public decimal IncrementoMinimo { get; private set; }
        public DateTime FechaInicio { get; private set; }
        public DateTime FechaFin { get; private set; }
        public EstadoSubasta Estado { get; private set; }


        public byte[] Version { get; private set; } = Array.Empty<byte>(); // EF Core usará esto para la concurrencia

        // Navegación
        public Usuario Vendedor { get; private set; } = null!;
        public Categoria Categoria { get; private set; } = null!;

        // 2. Proteger las colecciones: Exponemos IReadOnlyCollection
        private readonly List<Puja> _pujas = new();
        public IReadOnlyCollection<Puja> Pujas => _pujas.AsReadOnly();

        // 3. Constructor sin parámetros requerido por EF Core (privado/protegido)
        protected Subasta()
        {
            Titulo = string.Empty;
            Descripcion = string.Empty;
            UrlImagen = string.Empty;
        }

        // 4. Constructor de negocio: asegura que la entidad nace en un estado válido
        public Subasta(int vendedorId, int categoriaId, string titulo, string descripcion, string urlImagen, decimal precioBase, decimal incrementoMinimo, DateTime fechaInicio, DateTime fechaFin)
        {
            if (precioBase <= 0)
                throw new DomainException("El precio base debe ser mayor a cero.");

            if (fechaInicio >= fechaFin)
                throw new DomainException("La fecha de inicio debe ser anterior a la fecha de fin.");

            if (fechaFin <= DateTime.UtcNow)
                throw new DomainException("La fecha de fin debe ser en el futuro.");

            VendedorId = vendedorId;
            CategoriaId = categoriaId;
            Titulo = titulo;
            Descripcion = descripcion;
            UrlImagen = urlImagen;
            PrecioBase = precioBase;
            IncrementoMinimo = incrementoMinimo;
            FechaInicio = fechaInicio;
            FechaFin = fechaFin;
            Estado = fechaInicio > DateTime.UtcNow
                ? EstadoSubasta.Programada
                : EstadoSubasta.Activa;
        }

        // Mantiene compatibilidad con las subastas creadas internamente para datos de prueba.
        public Subasta(int vendedorId, int categoriaId, string titulo, string descripcion, string urlImagen, decimal precioBase, decimal incrementoMinimo, DateTime fechaFin)
            : this(vendedorId, categoriaId, titulo, descripcion, urlImagen, precioBase, incrementoMinimo, DateTime.UtcNow, fechaFin)
        {
        }

        // 5. Comportamiento (Reglas de Negocio)
        public void RegistrarPuja(int compradorId, decimal monto)
        {
            if (Estado != EstadoSubasta.Activa)
                throw new DomainException("Solo se pueden realizar pujas en subastas activas.");

            if (compradorId == VendedorId)
                throw new DomainException("El vendedor no puede pujar en su propia subasta.");

            if (DateTime.UtcNow > FechaFin)
                throw new DomainException("La subasta ya ha finalizado su tiempo.");

            decimal pujaMinima = _pujas.Any() ? _pujas.Max(p => p.Monto) + IncrementoMinimo : PrecioBase;

            if (monto < pujaMinima)
                throw new DomainException($"El monto de la puja debe ser al menos de ${pujaMinima}.");

            var nuevaPuja = new Puja
            {
                CompradorId = compradorId,
                Monto = monto,
                FechaPuja = DateTime.UtcNow,
                SubastaId = this.Id
            };

            // Regla Anti-Sniping
            var tiempoRestante = FechaFin - DateTime.UtcNow;
            if (tiempoRestante.TotalSeconds <= 60)
            {
                FechaFin = FechaFin.AddMinutes(2);
            }

            _pujas.Add(nuevaPuja);
        }
        // Métodos para que el Worker cambie los estados respetando el encapsulamiento
        public void IniciarSubastaProgramada()
        {
            if (Estado == EstadoSubasta.Programada)
            {
                Estado = EstadoSubasta.Activa;
            }
        }

        public void FinalizarConGanador()
        {
            Estado = EstadoSubasta.Finalizada;
        }

        public void DeclararDesierta()
        {
            Estado = EstadoSubasta.Desierta;
        }



        public void Finalizar()
        {
            if (DateTime.UtcNow < FechaFin)
                throw new DomainException("Aún no se ha cumplido el tiempo para finalizar la subasta.");

            Estado = _pujas.Any() ? EstadoSubasta.Finalizada : EstadoSubasta.Desierta;
        }
    }
}
