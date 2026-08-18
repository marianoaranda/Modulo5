# PRD FEAT-002: Soporte Docker (Dockerfiles + docker-compose)

| Field | Value |
|-------|-------|
| Ticket | FEAT-002 |
| Tracker | none |
| Date | 2026-08-18 |
| PRD loops | 0 |

## Context and Problem

Levantar el stack completo de Modulo5 (SQL Server 2017 + API + Web) para probar el sistema en local
requiere hoy instalar y configurar manualmente cada componente: instancia de SQL Server, ejecución de
las migraciones y el seed del perfil administrador, y las dos apps ASP.NET con sus variables de
entorno. Eso hace que levantar un entorno de prueba manual reproducible sea lento y dependiente de la
máquina de quien lo hace.

Se necesita una forma de levantar el stack completo (base de datos + API + Web) con un solo comando,
para pruebas manuales locales y para que cualquier persona del equipo reproduzca el mismo entorno.

## Goals

- Levantar el stack completo (`db`, `api`, `web`) con un único comando (`docker compose up`).
- Garantizar que la base de datos esté migrada y con el seed del administrador aplicado antes de que
  arranque la API.
- Mantener el contexto de build de las imágenes liviano, sin artefactos de build ni archivos de
  estado del repo.

## Functional Requirements

- FR-01: El sistema SHALL proveer un `Dockerfile` multi-stage para `Modulo5.Api` que compile con el
  SDK de .NET 8 y ejecute con la imagen runtime `aspnet:8.0`, exponiendo el puerto 8080 dentro del
  contenedor.
- FR-02: El sistema SHALL proveer un `Dockerfile` multi-stage para `Modulo5.Web` con el mismo patrón
  build/runtime que FR-01, exponiendo el puerto 8080 dentro del contenedor.
- FR-03: El sistema SHALL proveer un `docker-compose.yml` que defina cuatro servicios: `db` (SQL
  Server 2017), `db-init` (aplica migraciones y seed), `api` y `web`.
- FR-04: El servicio `db-init` SHALL crear la base `Modulo5` si no existe y ejecutar en orden los
  scripts `docker/seed/001-migrations.sql` y `docker/seed/002-seed-admin.sql`.
- FR-05: El servicio `api` SHALL depender de que `db-init` finalice exitosamente antes de arrancar.
- FR-06: El servicio `web` SHALL depender del servicio `api` y comunicarse con él mediante la red
  interna de Docker Compose (no vía `localhost`).
- FR-07: El sistema SHALL proveer un `.dockerignore` que excluya del contexto de build: `bin/`,
  `obj/`, `.vs/`, `.vscode/`, `.git/`, los archivos de estado de DAW (`.daw-state.json`,
  `.daw-paused/`, `.daw-sessions/`, `.daw-journal.jsonl`), `docs/` y `tests/`.
- FR-08: El sistema SHALL mapear los servicios `api` y `web` a puertos distintos del host (5080 y
  5000 respectivamente) para poder correrlos simultáneamente sin conflicto.

## Non-Functional Requirements

- NFR-01: Los datos de `db` SHALL persistir entre reinicios de contenedores mediante un volumen
  nombrado de Docker.
- NFR-02: Las credenciales usadas en `docker-compose.yml` (contraseña de SQL Server, clave de firma
  JWT) SHALL ser exclusivas de este entorno Docker de desarrollo/prueba manual y SHALL estar
  señaladas como tales con un comentario, nunca reutilizadas en un entorno real (mitigación del
  riesgo #1 del threat model de FEAT-001a sobre el origen de los secretos).
- NFR-03: El arranque completo del stack (`db` sano + `db-init` completo + `api` + `web` listos)
  SHALL completarse sin intervención manual más allá de `docker compose up`.

## Acceptance Criteria

- AC-01: WHEN el usuario ejecuta `docker compose up`, THE sistema SHALL levantar `db`, luego
  `db-init` (una vez que `db` esté saludable), luego `api` (una vez que `db-init` termine con éxito)
  y luego `web`. (FR-03, FR-05, FR-06)
- AC-02: WHEN `db-init` se ejecuta, THE sistema SHALL crear la base `Modulo5` si no existe y aplicar
  `001-migrations.sql` y `002-seed-admin.sql` en ese orden. (FR-04)
- AC-03: IF el contenedor `db` no reporta estado saludable dentro de los reintentos configurados,
  THEN THE servicio `db-init` SHALL no iniciar. (FR-03)
- AC-04: WHEN el contenedor `api` arranca, THE sistema SHALL exponerlo en el puerto 5080 del host.
  (FR-01, FR-08)
- AC-05: WHEN el contenedor `web` arranca, THE sistema SHALL exponerlo en el puerto 5000 del host y
  SHALL alcanzar la API en `http://api:8080/` por la red interna de Compose. (FR-02, FR-06, FR-08)
- AC-06: IF el contexto de build incluye `bin/`, `obj/`, `.vs/`, `.vscode/`, `.git/`, archivos de
  estado de DAW, `docs/` o `tests/`, THEN THE `.dockerignore` SHALL excluirlos de las imágenes.
  (FR-07)

## Out of Scope

- Despliegue a un entorno productivo (orquestación, TLS/HTTPS, balanceo).
- Publicación de las imágenes a un registry.
- Integración con un pipeline de CI/CD.
- Gestión de secretos vía un vault o servicio externo — las credenciales de este PRD son
  exclusivamente para uso local de desarrollo/prueba manual.
- Configuración de Kubernetes o cualquier otro orquestador.

## Risks and Mitigations

- **Riesgo:** las credenciales embebidas en `docker-compose.yml` (contraseña SA, clave de firma JWT)
  podrían copiarse por error a un entorno real.
  **Mitigación:** comentario explícito en el `docker-compose.yml` señalando que son exclusivas de
  este entorno Docker de prueba local y que los valores reales de producción siguen viniendo solo de
  `user-secrets`/variables de entorno del host real (NFR-02).
- **Riesgo:** el puerto 1433 de SQL Server queda expuesto al host, ampliando la superficie de ataque
  si la máquina no está aislada.
  **Mitigación:** uso exclusivo para entornos de desarrollo/prueba manual local, nunca para un
  entorno accesible desde red pública (Out of Scope).

## Dependencies

- `Modulo5.Api` y `Modulo5.Web` (proyectos existentes, .NET 8).
- Imagen `mcr.microsoft.com/mssql/server:2017-latest` (SQL Server 2017, conforme al stack declarado
  en `AGENTS.md`).
- Scripts de migración y seed existentes (`docker/seed/001-migrations.sql`,
  `docker/seed/002-seed-admin.sql`).
- Threat model de FEAT-001a (`docs/daw/security/threat-model-FEAT-001a.md` o equivalente) — origen
  de la mitigación referenciada en NFR-02 sobre el manejo de secretos.
