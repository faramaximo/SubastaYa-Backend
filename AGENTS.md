# AGENTS.md - Directrices de Arquitectura y Desarrollo con IA

Este documento define el contexto técnico, los principios arquitectónicos y las directrices obligatorias para cualquier agente inteligente o desarrollador que contribuya a la base de código de **SubastaYa**.

---

## 1. Stack Tecnológico

- **Plataforma:** .NET 8 (C# 12)
- **Acceso a Datos / ORM:** Entity Framework Core 8 con Pomelo MySQL (Pomelo.EntityFrameworkCore.MySql)
- **Base de Datos:** MySQL Server 8.0+
- **Autenticación:** JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer) con claims tipados
- **Hashing de Contraseñas:** BCrypt (BCrypt.Net-Next)
- **Comunicación en Tiempo Real:** ASP.NET Core SignalR (/hubs/auction)
- **Background Worker:** BackgroundService (AuctionStatusWorker) para finalización programada de subastas
- **Documentación API:** OpenAPI / Swagger (Swashbuckle.AspNetCore)
- **Testing:** xUnit, Moq, FluentAssertions

---

## 2. Arquitectura en 4 Capas (Clean Architecture)

El backend implementa una arquitectura limpia y desacoplada con estricto flujo unidireccional de dependencias:

`
┌────────────────────────────────────────────────────────┐
│             SubastaYa.WebApi (Presentación)             │
│   Controllers, Middlewares, Hubs, Workers, Program.cs  │
└──────────────────────────┬─────────────────────────────┘
                           │ referencia
                           ▼
┌────────────────────────────────────────────────────────┐
│             SubastaYa.Infrastructure (Infra)           │
│   DbContext, Configurations, Repositories, Services    │
└──────────────────────────┬─────────────────────────────┘
                           │ referencia
                           ▼
┌────────────────────────────────────────────────────────┐
│             SubastaYa.Application (Casos de Uso)       │
│   Handlers, Commands, Queries, DTOs, Interfaces        │
└──────────────────────────┬─────────────────────────────┘
                           │ referencia
                           ▼
┌────────────────────────────────────────────────────────┐
│             SubastaYa.Domain (Núcleo de Dominio)       │
│   Entities, Enums, Exceptions, Domain Rules            │
└────────────────────────────────────────────────────────┘
`

### Regla de Dependencias
- **Domain**: Es el núcleo central. **NO** tiene dependencias hacia ninguna otra capa ni paquetes externos de persistencia o frameworks.
- **Application**: Depende únicamente de Domain. Define contratos (Interfaces), modelos de transferencia (DTOs) y orquesta casos de uso (Commands, Queries, Handlers). **NO** depende de Infrastructure ni de WebApi.
- **Infrastructure**: Implementa las abstracciones definidas en Application (IRepository, IUnitOfWork, IEmailSender, IAuditService). Gestiona SubastaYaDbContext, migraciones y configuraciones de EF Core.
- **WebApi**: Capa exterior / punto de entrada HTTP y WebSockets. Recibe peticiones, valida modelos DTO, delega en los Handlers de Application y serializa respuestas HTTP.

---

## 3. Reglas de Diseño y Convenciones Obligatorias

1. **Responsabilidad de los Controllers**:
   - Los controladores HTTP deben ser delgados (*skinny controllers*).
   - Queda terminantemente prohibido inyectar DbContext, generar tokens JWT o ejecutar llamadas SMTP/Email dentro de los controladores.
   - Cada acción del controller únicamente mapea parámetros/DTOs, invoca el Handler correspondiente y retorna el ActionResult adecuado.

2. **Manejo de Transacciones y Persistencia (Unit of Work)**:
   - Los repositorios **NO** deben llamar a SaveChangesAsync() internamente.
   - La persistencia atómica se coordina exclusivamente mediante la abstracción IUnitOfWork (SaveChangesAsync(), BeginTransactionAsync(), CommitAsync(), RollbackAsync()).

3. **Inmutabilidad en el Dominio**:
   - Los registros de pistas de auditoría (AuditoriaLog) y asientos contables (TransaccionLedger) deben garantizar inmutabilidad mediante propiedades con acceso init o private set.

4. **Configuraciones de Entity Framework Core**:
   - No utilizar mapeos Fluent API inline dentro de OnModelCreating en SubastaYaDbContext.
   - Cada entidad debe tener su propia clase de configuración que implemente IEntityTypeConfiguration<T> ubicada en Infrastructure/Data/Configurations/.
   - SubastaYaDbContext debe cargar las configuraciones automáticamente mediante ApplyConfigurationsFromAssembly().

5. **Manejo Centralizado de Excepciones**:
   - Los errores de dominio (DomainException), problemas de autorización (UnauthorizedException) y conflictos de concurrencia (DbUpdateConcurrencyException, ConcurrencyException) deben ser gestionados por el middleware global (ExceptionMiddleware), mapeando a los códigos HTTP correspondientes (400, 401, 409).

6. **Concurrencia Optimista**:
   - Subasta y Billetera cuentan con control de concurrencia (IsRowVersion() / IsConcurrencyToken()) para evitar carreras críticas en pujas simultáneas y transacciones financieras.
