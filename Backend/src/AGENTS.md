# AGENTS.md · Convenciones del Proyecto SubastaYa

## 1. Stack Tecnológico
- **Framework**: .NET 8 Web API
- **ORM**: Entity Framework Core 8 (Pomelo MySQL)
- **Base de Datos**: MySQL 8.0 corriendo en Docker Compose
- **Real-Time**: SignalR
- **Testing**: xUnit + Moq / NSubstitute

## 2. Arquitectura y Reglas de Capas (Clean Architecture)
El proyecto está dividido en 4 capas estrictas. La dirección de las dependencias va SIEMPRE hacia el centro:

- **SubastaYa.Domain**: Contiene Entidades Ricas, Value Objects, Enums y Excepciones de Negocio (`DomainException`).
  - *REGLA*: NO referencia a ningún otro proyecto. NO conoce HTTP, EF Core ni SQL.
  - *REGLA*: Las entidades protegen su estado (propiedades con `private set`). Las reglas de negocio viven en métodos dentro de la entidad (ej: `billetera.RetenerSaldo(monto)`).

- **SubastaYa.Application**: Contiene Casos de Uso, Servicios, DTOs e Interfaces de Repositorios / UoW.
  - *REGLA*: La lógica de orquestación vive acá (`BidService`, `WalletService`), NUNCA en los Controllers.
  - *REGLA*: Hacia afuera (Presentation) solo viajan DTOs, jamás Entidades del Dominio.

- **SubastaYa.Infrastructure**: Implementación técnica (DbContext, Mappings, Repositorios concretos, Email, SignalR Hubs).
  - *REGLA*: Los repositorios preparan los cambios pero NUNCA llaman a `SaveChangesAsync()` por su cuenta. La persistencia la decide el caso de uso a través de `IUnitOfWork`.

- **SubastaYa.WebApi (Presentation)**: Controllers, Middlewares, Program.cs (Composition Root).
  - *REGLA*: Los Controllers solo reciben HTTP, invocan al servicio/handler de Application y devuelven la respuesta HTTP. No hay bloques `try/catch` manuales para reglas de negocio (se usa `ExceptionMiddleware`).

## 3. Manejo de Errores y Validaciones
- Errores de negocio: Lanzar `DomainException("Mensaje explicativo")` desde el Dominio o Aplicación.
- El `ExceptionMiddleware` captura `DomainException` y la traduce automáticamente a `HTTP 400 Bad Request`.
- Errores no controlados se capturan y traducen a `HTTP 500 Internal Server Error`.

## 4. Convenciones de Código
- Código y comentarios en Español para la lógica de negocio (ej: `Billetera`, `Pujas`, `Subasta`).
- Métodos asincrónicos terminan en `Async` y devuelven `Task` o `Task<T>`.
- Consultas de solo lectura en EF Core deben usar `.AsNoTracking()`.
- Registros de Inyección de Dependencias:
  - `DbContext` y Repositorios: **Scoped**
  - Unidades de Trabajo (`IUnitOfWork`): **Scoped**
  - Servicios de Lógica de Negocio: **Scoped**
  - Componentes sin estado / Utilidades: **Transient** o **Singleton**

## 5. Criterio de Aceptación
Antes de entregar o dar por terminada una tarea:
1. Ejecutar `dotnet build` y verificar 0 errores de compilación.
2. Ejecutar `dotnet test` y asegurar que todos los tests unitarios pasen.