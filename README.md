# SubastaYa - Plataforma de Subastas en Tiempo Real

SubastaYa es un sistema distribuido de subastas en tiempo real construido sobre **.NET 8** y **Entity Framework Core 8**, estructurado bajo los principios de **Clean Architecture** (Arquitectura en 4 capas).

---

## 1. Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL Server 8.0+](https://dev.mysql.com/downloads/mysql/) o contenedor Docker equivalente
- Servidor SMTP para pruebas locales o cuenta en [Mailtrap](https://mailtrap.io/)
- Cliente HTTP / curl o terminal Bash para ejecucion de pruebas

---

## 2. Configuracion Inicial del Entorno

1. En la carpeta `Backend/src/SubastaYa.WebApi/`, copia la plantilla de configuracion:
   ```bash
   cp appsettings.Example.json appsettings.json
   ```
2. Configura los parametros de tu entorno local en `appsettings.json`:
   - Cadena de conexion MySQL (`DefaultConnection`).
   - Clave secreta JWT (`Jwt:SecretKey`, minimo 32 caracteres).
   - Credenciales SMTP para envio de correos (`Email:Username`, `Email:Password`).

---

## 3. Compilacion, Migraciones y Ejecucion

### Compilar la Solucion
Desde la raiz del repositorio:
```bash
dotnet build Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj
```

### Aplicar Migraciones a la Base de Datos
Para crear o actualizar el esquema relacional en MySQL:
```bash
dotnet ef database update --project Backend/src/SubastaYa.Infrastructure --startup-project Backend/src/SubastaYa.WebApi
```

*(Nota: Al arrancar la aplicacion en modo desarrollo, el seeder `DbInitializer` poblara datos iniciales de catalogo y usuarios de prueba si la base de datos se encuentra vacia).*

### Ejecutar la Web API
```bash
dotnet run --project Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj
```
Por defecto la API estara escuchando en:
- API Base: `http://localhost:5216`
- Swagger UI: `http://localhost:5216/swagger`
- Interfaz Web SPA: `http://localhost:5216/index.html`
- Hub de SignalR: `http://localhost:5216/hubs/auction`

---

## 4. Prueba de Estres y Concurrencia (Stress Test)

El backend implementa **concurrencia optimista** (mediante RowVersion / Concurrency Tokens y reglas de dominio) para garantizar la consistencia en pujas simultaneas y evitar condiciones de carrera.

### Ejecucion del Script de Estres
En la raiz del repositorio ejecuta:
```bash
chmod +x stress-test.sh
./stress-test.sh
```
*(Puedes configurar las variables `API_URL`, `TOKEN`, `SUBASTA_ID` y `MONTO` mediante variables de entorno).*

### Demostracion del Control de Concurrencia
El script lanza **2 peticiones POST identicas en el mismo milisegundo** contra la misma subasta:
```bash
curl -i -X POST http://localhost:5216/api/v1/bids ... &
curl -i -X POST http://localhost:5216/api/v1/bids ... &
```

**Comportamiento esperado y verificado:**
1. **Peticion Ganadora:**
   - La primera transaccion en confirmarse retiene el saldo de la billetera, registra el asiento contable inmutable en `TransaccionLedger`, actualiza la puja lider y la version de la subasta.
   - **Respuesta:** `HTTP 200 OK` con el payload de la puja confirmada.
2. **Peticion Concurrente (Perdedora):**
   - La segunda transaccion detecta un conflicto de concurrencia (`DbUpdateConcurrencyException` o regla de negocio).
   - El `ExceptionMiddleware` captura la excepcion, audita el conflicto en `AuditoriaLog` y rechaza la transaccion.
   - **Respuesta:** `HTTP 409 Conflict` con el mensaje:
     ```json
     {
       "statusCode": 409,
       "message": "Alguien mas realizo una puja al mismo tiempo. Actualiza la subasta y vuelve a intentarlo."
     }
     ```

---

## 5. Estructura del Repositorio

- **AGENTS.md:** Principios de diseno, stack tecnologico y directrices de arquitectura en 4 capas.
- **PROMPTS.md:** Registro cronologico de sesiones de desarrollo y bitacora de prompts con IA.
- **stress-test.sh:** Script en Bash para verificacion de concurrencia optimista y colisiones HTTP 409.
- **Backend/src/SubastaYa.Domain:** Entidades puras, excepciones de dominio, inmutabilidad y reglas de negocio.
- **Backend/src/SubastaYa.Application:** Casos de uso (Commands/Queries/Handlers), interfaces y DTOs.
- **Backend/src/SubastaYa.Infrastructure:** Entity Framework Core, mapeos `IEntityTypeConfiguration<T>`, repositorios y SMTP.
- **Backend/src/SubastaYa.WebApi:** Controladores RESTful delgados, SignalR Hub, Background Worker y Middlewares.