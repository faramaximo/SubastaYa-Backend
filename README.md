# SubastaYa - Plataforma de Subastas en Tiempo Real

SubastaYa es un sistema distribuido de subastas en tiempo real construido sobre **.NET 8** y **Entity Framework Core 8**, estructurado bajo los principios de **Clean Architecture** (Arquitectura en 4 capas).

---

## 1. Requisitos Previos

- [Docker](https://www.docker.com/) y Docker Compose (recomendado para ejecución completa y reproducible)
- Alternativamente para ejecución nativa:
  - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
  - [MySQL Server 8.0+](https://dev.mysql.com/downloads/mysql/)
  - Servidor SMTP local (Mailpit)
  - Cliente HTTP / curl o terminal Bash para ejecución de pruebas

---

## 2. Ejecución Rápida con Docker Compose (Recomendado)

El proyecto cuenta con un entorno contenerizado reproducible que levanta la base de datos MySQL 8, el servidor SMTP local con interfaz web Mailpit y la API Web en ASP.NET Core 8.

### Iniciar todo el ecosistema:
Desde la raíz del repositorio:
```bash
docker compose up --build
```
*(O en segundo plano con `docker compose up --build -d`)*

### URLs de Acceso a los Servicios:
- **API y Documentación Swagger UI:** [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Interfaz Web (SPA):** [http://localhost:5000/index.html](http://localhost:5000/index.html)
- **Bandeja de Entrada SMTP (Mailpit Web UI):** [http://localhost:8025](http://localhost:8025)
- **Base de Datos MySQL:** `localhost:3306` (Usuario: `subastaya_user`, Contraseña: `subastaya_password`, Base: `SubastaYaDb`)
- **Hub en Tiempo Real (SignalR):** [http://localhost:5000/hubs/auction](http://localhost:5000/hubs/auction)

### Detener los Servicios:
```bash
docker compose down
```
*(Para reiniciar la base de datos desde cero eliminando los volúmenes persistentes, usa `docker compose down -v`).*

---

## 3. Configuración Inicial para Ejecución Local (Sin Docker)

1. En la carpeta `Backend/src/SubastaYa.WebApi/`, copia la plantilla de configuración:
   ```bash
   cp appsettings.Example.json appsettings.json
   ```
2. Configura los parámetros de tu entorno local en `appsettings.json`:
   - Cadena de conexión MySQL (`DefaultConnection`).
   - Clave secreta JWT (`Jwt:SecretKey`, mínimo 32 caracteres).
   - Configuración SMTP para Mailpit (`Email:SmtpHost: "mailpit"`, `Port: 1025`, `UseSsl: false`).

---

## 4. Compilación, Migraciones y Ejecución Local

### Compilar la Solución
Desde la raíz del repositorio:
```bash
dotnet build Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj
```

### Aplicar Migraciones a la Base de Datos
Para crear o actualizar el esquema relacional en MySQL:
```bash
dotnet ef database update --project Backend/src/SubastaYa.Infrastructure --startup-project Backend/src/SubastaYa.WebApi
```

*(Nota: Al arrancar la aplicación en modo desarrollo, el seeder `DbInitializer` poblará datos iniciales de catálogo y usuarios de prueba si la base de datos se encuentra vacía).*

### Ejecutar la Web API de forma local
```bash
dotnet run --project Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj
```
Por defecto la API estará escuchando en:
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