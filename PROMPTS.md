# PROMPTS.md - Bitácora de Interacciones con la IA

Este documento registra cronológicamente los requerimientos solicitados, el rol asumido por el asistente de IA, las acciones ejecutadas y el resultado de las validaciones de compilación.

---

## Sesión de Refactorización y Arquitectura Limpia

### Iteración 1: Refactorización Inicial de Controllers y Entidades
- **Rol asignado:** Desarrollador Senior en .NET 8.
- **Instrucciones clave:**
  - En UsersController y AuthController: Eliminar lógica no correspondiente a presentación (envío de correos, generación de tokens, consultas directas a BD).
  - En TransaccionLedger y AuditoriaLog: Establecer inmutabilidad en propiedades (init / private set).
  - En SubastaYaDbContext: Extraer Fluent API inline a clases IEntityTypeConfiguration<T> bajo Infrastructure/Data/Configurations/.
- **Acciones ejecutadas:**
  - Análisis de dependencias y flujo de casos de uso existentes.
  - Generación de configuraciones separadas para Billetera, Subasta, Usuario y TransaccionLedger.
  - Configuración de inmutabilidad con descriptores init en entidades de ledger y auditorña.
- **Validación:**
  - Comando: dotnet build Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj
  - Resultado: Exitoso (0 Errores, 0 Advertencias).

---

### Iteración 2: Extracción y Consolidación de Fluent API
- **Rol asignado:** Desarrollador Senior en .NET 8 (Scope restringido a entidades).
- **Instrucciones clave:**
  - Modificar exclusivamente TransaccionLedger.cs, AuditoriaLog.cs y SubastaYaDbContext.cs.
  - Convertir setters a init para garantizar inmutabilidad en asientos contables y logs.
  - Extraer configuraciones de entidades a Infrastructure/Data/Configurations/.
- **Acciones ejecutadas:**
  - Implementación de clases IEntityTypeConfiguration<T>:
    - SubastaConfiguration (precisión de decimales, token de concurrencia y clave de índice compuesto para worker).
    - BilleteraConfiguration (precisión de decimales y token de concurrencia UUID).
    - TransaccionLedgerConfiguration (conversión a string y longitud del enum TipoTransacción).
    - UsuarioConfiguration.
  - Limpieza de SubastaYaDbContext.cs invocando modelBuilder.ApplyConfigurationsFromAssembly().
- **Validación:**
  - Comando: dotnet build Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj
  - Resultado: Exitoso (0 Errores, 0 Advertencias).

---

### Iteración 3: Verificación de Pureza en Controladores WebApi
- **Rol asignado:** Desarrollador Senior en .NET 8.
- **Instrucciones clave:**
  - Inspeccionar UsersController y AuthController para asegurar que dependan exclusivamente de Handlers/Servicios inyectados.
  - Eliminar código muerto o consultas directas a DbContext.
- **Acciones ejecutadas:**
  - Auditoría de dependencias en UsersController y AuthController. Se comprobó que las operaciones delegan en Handlers de Application (RegisterUserCommandHandler, LoginQueryHandler, GetMisPublicacionesQueryHandler, GetMisPujasQueryHandler).
- **Validación:**
  - Comando: dotnet build Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj
  - Resultado: Exitoso (0 Errores, 0 Advertencias).

---

### Iteración 4: Seguridad, Documentación y Pruebas de Estrés
- **Rol asignado:** Tech Lead del Proyecto.
- **Instrucciones clave:**
  - Sanitizar credenciales en appsettings.json y crear appsettings.Example.json.
  - Crear AGENTS.md detallando stack y lineamientos arquitectónicos.
  - Crear PROMPTS.md con la bitácora de prompts y validaciones.
  - Corregir tipografía de READNE.md a README.md con guía paso a paso de ejecución y explicación de concurrencia.
  - Crear script Bash stress-test.sh demostrando el manejo de concurrencia con respuestas 200 OK y 409 Conflict.
- **Acciones ejecutadas:**
  - Eliminación de contraseñas y claves privadas reales de configuración.
  - Redacción de documentación técnica y scripts de prueba.
- **Validación:**
  - Compilación final: dotnet build Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj -> Exitoso.
