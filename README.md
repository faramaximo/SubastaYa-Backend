# SubastaYa — Plataforma de Subastas en Tiempo Real

Plataforma web de subastas en tiempo real construida con **.NET 8**, **Entity Framework Core 8** y **SignalR**, siguiendo los principios de **Clean Architecture** (Domain / Application / Infrastructure / WebApi).

---

## Requisitos Previos

| Herramienta | Versión mínima |
|---|---|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | 4.x |
| [Git](https://git-scm.com/) | 2.x |

> No se requiere instalar .NET SDK, MySQL ni ninguna otra dependencia de forma local. Todo corre dentro de contenedores.

---

## Quickstart

### 1. Levantar el ecosistema completo

Desde la raíz del repositorio:

```bash
docker compose up --build
```

La primera ejecución descarga las imágenes, aplica las migraciones y carga los datos semilla automáticamente. Cuando el log muestre `Now listening on: http://[::]:8080`, el sistema está listo.

### 2. URLs de acceso

| Servicio | URL |
|---|---|
| Aplicación Web (SPA) | http://localhost:5000/index.html |
| Documentación API (Swagger UI) | http://localhost:5000/swagger |
| Bandeja SMTP — Mailpit Web UI | http://localhost:8025 |

### 3. Apagar y limpiar

```bash
# Detener los contenedores y eliminar volúmenes (reinicia la BD desde cero)
docker compose down -v
```

---

## Credenciales y Datos Semilla

Todos los usuarios tienen la misma contraseña de prueba: **`123456`**

| Email | Rol | Saldo Total | Saldo Retenido | Saldo Disponible |
|---|---|---|---|---|
| `vendedor@test.com` | Vendedor | \$0 | \$0 | \$0 |
| `comprador1@test.com` | Postor líder | \$150.000 | \$45.000 | \$105.000 |
| `comprador2@test.com` | Postor habilitado | \$200.000 | \$0 | \$200.000 |
| `sinfondos@test.com` | Sin fondos | \$500 | \$0 | \$500 |

### Subastas de Prueba (Seed)

| # | Descripción | Estado |
|---|---|---|
| 1 | Subasta activa estándar — cierra en ~20-30 min, 2 pujas previas, líder \$45.000 | Activa |
| 2 | Subasta activa crítica — cierra en <2 min (prueba alerta visual + anti-sniping) | Activa |
| 3 | Subasta próxima — inicio programado a +24 hs, pujas bloqueadas | Próxima |
| 4 | Subasta vencida con ganador — liquidación por el Worker | Finalizada |
| 5 | Subasta vencida desierta — sin pujas, estado DESIERTA | Desierta |

---

## Prueba de Concurrencia (Stress Test)

El backend implementa **Optimistic Locking** (campo `RowVersion` / Concurrency Token en EF Core) para rechazar colisiones de puja y garantizar que solo una transacción gane en caso de escrituras simultáneas.

### Ejecución (desde Git Bash en Windows o cualquier terminal Bash/Unix)

```bash
chmod +x stress-test.sh
./stress-test.sh
```

El script dispara **dos peticiones `POST /api/v1/auctions/{id}/bids` idénticas en paralelo** (`&`) usando tokens JWT de `comprador1` y `comprador2`.

### Comportamiento esperado

| Petición | Resultado | Código HTTP |
|---|---|---|
| Ganadora (primera en confirmar) | Puja registrada, saldo retenido, Ledger actualizado | `201 Created` |
| Perdedora (detecta conflicto de concurrencia) | Transacción rechazada, evento auditado en `AuditoriaLog` | `409 Conflict` |

Cuerpo de la respuesta perdedora:

```json
{
  "statusCode": 409,
  "message": "Alguien más realizó una puja al mismo tiempo. Actualiza la subasta y vuelve a intentarlo."
}
```

> Las variables `API_URL`, `TOKEN_POSTOR1`, `TOKEN_POSTOR2`, `SUBASTA_ID` y `MONTO` son sobreescribibles vía entorno para apuntar a subastas específicas o usar tokens frescos.

---

## Estructura del Repositorio

```
SubastaYa/
├── Backend/
│   └── src/
│       ├── SubastaYa.Domain/          # Entidades, excepciones de dominio, reglas de negocio
│       ├── SubastaYa.Application/     # Commands, Queries, Handlers, DTOs, interfaces
│       ├── SubastaYa.Infrastructure/  # EF Core, repositorios, SMTP (Mailpit)
│       └── SubastaYa.WebApi/          # Controladores REST, SignalR Hub, Worker, Middlewares
├── Fronted/                           # SPA — HTML/CSS/JS
├── docker-compose.yml                 # Orquestación: API + MySQL + Mailpit
├── Dockerfile                         # Imagen multi-stage de la WebApi
├── stress-test.sh                     # Script Bash — prueba de concurrencia optimista
└── stress-test.ps1                    # Equivalente PowerShell para Windows nativo
```