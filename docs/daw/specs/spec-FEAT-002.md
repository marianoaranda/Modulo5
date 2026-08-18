# Spec FEAT-002: Soporte Docker (Dockerfiles + docker-compose)

| Field | Value |
|-------|-------|
| Ticket | FEAT-002 |
| PRD | docs/daw/prd/prd-FEAT-002.md |
| Tier | FEATURE |
| Date | 2026-08-18 |
| Spec loops | 0 |

## Summary

Formalizar y finalizar los archivos Docker que ya existen en el working tree sin commitear
(`Dockerfile` de Api y Web, `docker-compose.yml`, `.dockerignore`), aplicando las mitigaciones que
salieron del threat model (`docs/daw/security/threat-FEAT-002.md`): bindear el puerto de SQL Server
a `127.0.0.1`, externalizar las credenciales de desarrollo a un `.env` gitignoreado, y ampliar
`.dockerignore` para alinearlo con `.gitignore`.

## Coverage: PRD → blocks

| Requirement | Covered by |
|---|---|
| FR-01 | Block 1 |
| FR-02 | Block 1 |
| FR-03 | Block 2 |
| FR-04 | Block 2 |
| FR-05 | Block 2 |
| FR-06 | Block 2 |
| FR-07 | Block 3 |
| FR-08 | Block 2 |
| NFR-01 | Strategy: volumen nombrado `modulo5-db-data` en Block 2 |
| NFR-02 | Strategy: comentario explícito en `docker-compose.yml` (ya presente) + externalización a `.env` gitignoreado en Block 2 (mitigación del riesgo #2 del threat model) |
| NFR-03 | Strategy: cadena `depends_on` con `condition: service_healthy` / `service_completed_successfully` en Block 2 |

## Dependencies between blocks

Block 1 y Block 3 son independientes entre sí. Block 2 depende de que Block 1 exista (el
`docker-compose.yml` referencia ambos `Dockerfile` por ruta) para poder verificarse con un build
real. Orden sugerido: **1 → 3 → 2** (dejar el contexto de build acotado antes del primer build), pero
1 y 3 pueden hacerse en cualquier orden entre sí.

## Block 1 — Dockerfiles de Api y Web

**Files**
- `src/Modulo5.Api/Dockerfile` (existe, sin commitear — finalizar)
- `src/Modulo5.Web/Dockerfile` (existe, sin commitear — finalizar)

**Logic**
Build multi-stage: etapa `build` con `mcr.microsoft.com/dotnet/sdk:8.0`, etapa `runtime` con
`mcr.microsoft.com/dotnet/aspnet:8.0`. `Modulo5.Api/Dockerfile` copia y restaura
`Modulo5.Domain`, `Modulo5.Data` y `Modulo5.Api` (coincide con las `ProjectReference` reales de
`Modulo5.Api.csproj`, confirmado por el impact scan y el arch-auditor). `Modulo5.Web/Dockerfile`
copia y restaura únicamente `Modulo5.Web` (el `.csproj` declara explícitamente que no depende de
Domain/Data). Ambos exponen `ASPNETCORE_URLS=http://+:8080` y `EXPOSE 8080`.

**Input validation**
No aplica (no hay entrada de usuario en este bloque).

**Error handling**
No aplica: no hay lógica de manejo de errores propia de este bloque (el comportamiento de
`docker build` ante un fallo de `dotnet restore`/`publish` es nativo de Docker).

**Required tests**
- [ ] `docker build -f src/Modulo5.Api/Dockerfile -t modulo5-api-test .` completa sin errores —
      valida FR-01.
- [ ] `docker build -f src/Modulo5.Web/Dockerfile -t modulo5-web-test .` completa sin errores —
      valida FR-02.

**Completion criterion**
Ambos `docker build` completan exitosamente y las imágenes resultantes contienen únicamente los
artefactos publicados (`dotnet publish -c Release`), sin código fuente de más.

## Block 2 — docker-compose.yml + externalización de credenciales

**Files**
- `docker-compose.yml` (existe, sin commitear — finalizar + aplicar mitigaciones)
- `.env.example` (nuevo) — documenta las variables `SA_PASSWORD` y `JWT_SIGNING_KEY` con valores
  placeholder, se commitea.
- `.env` (nuevo, local, NO se commitea) — valores reales de desarrollo, debe estar gitignoreado.
- `.gitignore` (modificado) — agregar `.env` a las exclusiones.

**Logic**
Cuatro servicios: `db` (`mcr.microsoft.com/mssql/server:2017-latest`, healthcheck vía `sqlcmd`),
`db-init` (efímero, `depends_on: db: condition: service_healthy`, crea la base `Modulo5` si no
existe y aplica `docker/seed/001-migrations.sql` y `002-seed-admin.sql` en ese orden), `api`
(`depends_on: db-init: condition: service_completed_successfully`), `web` (`depends_on: api`,
alcanza la Api vía `http://api:8080/` por la red interna de Compose). Volumen nombrado
`modulo5-db-data` para persistir los datos de `db` entre reinicios (NFR-01). Puertos host: `db` →
**`127.0.0.1:1433:1433`** (mitigación riesgo #1 del threat model — restringe el acceso a la propia
máquina), `api` → `5080:8080`, `web` → `5000:8080`.

Las variables `SA_PASSWORD` y `Jwt__SigningKey`/`JWT_SIGNING_KEY` dejan de estar hardcodeadas: se
referencian como `${SA_PASSWORD}` y `${JWT_SIGNING_KEY}` en `docker-compose.yml`, y Compose las
resuelve automáticamente desde un `.env` en la raíz del repo (mecanismo nativo de Docker Compose,
sin necesidad de `env_file` explícito). El `.env` real es local y gitignoreado; `.env.example`
documenta las claves esperadas con placeholders (ej. `SA_PASSWORD=changeme-local-only`) para que
cualquiera pueda crear su propio `.env` copiándolo. El comentario ya existente en
`docker-compose.yml` sobre el origen de los secretos (mitigación riesgo #1 del threat model de
FEAT-001a) se mantiene.

**Error handling**
Si `db` no reporta `healthy` dentro de los reintentos configurados, `db-init` no arranca
(comportamiento nativo de `depends_on: condition: service_healthy`, ya cubierto por AC-03). Si
`.env` no existe, Compose falla la interpolación de variables con un error explícito al momento de
`docker compose up` — no hay fallback silencioso a un valor vacío (ver test dedicado abajo).

**Required tests**
- [ ] `docker compose config` resuelve sin errores de sintaxis ni variables faltantes (con un `.env`
      de prueba presente) — valida FR-03, FR-08.
- [ ] `docker compose up` levanta `db` → `db-init` → `api` → `web` en ese orden y los cuatro
      contenedores terminan en estado `running`/`exited (0)` según corresponda — valida AC-01,
      AC-02, AC-03.
- [ ] El puerto 1433 solo responde en `127.0.0.1`, no en la IP de red del host — valida la
      mitigación del riesgo #1.
- [ ] Una petición HTTP a `http://localhost:5080/` responde desde el host — valida AC-04.
- [ ] Una petición HTTP a `http://localhost:5000/` responde desde el host y la Web alcanza la Api
      internamente — valida AC-05.
- [ ] `git status` no muestra `.env` como archivo trackeable (queda ignorado) y sí muestra
      `.env.example` como nuevo archivo a commitear — valida la mitigación del riesgo #2.
- [ ] `docker compose up` sin un `.env` presente falla con un error explícito de variable no
      resuelta (no arranca ningún contenedor con un valor vacío) — valida el manejo de error de
      `.env` ausente.

**Completion criterion**
`docker compose up` levanta el stack completo con un solo comando, sin intervención manual, con las
credenciales de desarrollo fuera del archivo versionado.

## Block 3 — .dockerignore

**Files**
- `.dockerignore` (existe, sin commitear — finalizar)

**Logic**
Excluir del contexto de build: `**/bin/`, `**/obj/`, `**/.vs/`, `**/.vscode/`, `.git/`,
`.daw-state.json`, `.daw-paused/`, `.daw-sessions/`, `.daw-journal.jsonl`, `docs/`, `tests/` (ya
presentes) y agregar `*.user` y `appsettings.*.local.json` para alinearlo con `.gitignore`
(mitigación del riesgo #6 del threat model).

**Error handling**
No aplica.

**Required tests**
- [ ] Un archivo `appsettings.Development.local.json` de prueba creado en `src/Modulo5.Api/` no
      aparece dentro del contexto de build (`docker build` con `--progress=plain` no lo procesa) —
      valida FR-07/AC-06 y la mitigación del riesgo #6.

**Completion criterion**
El contexto de build de ambas imágenes no contiene artefactos de compilación, archivos de estado de
DAW, documentación, tests, ni patrones de configuración local sensibles.

## Final verification

- `docker compose up` levanta el stack completo (`db`, `db-init`, `api`, `web`) con un único
  comando, en el orden `db` (healthy) → `db-init` (completo) → `api` → `web`.
- `api` responde en `http://localhost:5080/` y `web` en `http://localhost:5000/`, y `web` alcanza a
  `api` por la red interna de Compose.
- El puerto de `db` (1433) solo es alcanzable desde `127.0.0.1`.
- Ningún secreto de desarrollo queda hardcodeado en un archivo versionado: `docker-compose.yml`
  usa `${SA_PASSWORD}`/`${JWT_SIGNING_KEY}`, `.env` está gitignoreado, `.env.example` documenta las
  claves con placeholders.
- `.dockerignore` excluye build artifacts, archivos de estado de DAW, docs, tests y patrones de
  configuración local sensible (`*.user`, `appsettings.*.local.json`).
