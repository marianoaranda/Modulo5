# Threat Model FEAT-002: Soporte Docker

| Field | Value |
|-------|-------|
| Ticket | FEAT-002 |
| Date | 2026-08-18 |
| Spec | docs/daw/specs/spec-FEAT-002.md (a escribir tras este reporte) |
| PRD | docs/daw/prd/prd-FEAT-002.md |

## Componentes analizados

1. `docker-compose.yml` — orquestación de 4 servicios: `db`, `db-init`, `api`, `web`.
2. `db` — contenedor `mcr.microsoft.com/mssql/server:2017-latest`, publica el puerto 1433 al host.
3. `db-init` — contenedor efímero que aplica `docker/seed/001-migrations.sql` y
   `002-seed-admin.sql` contra `db` usando la cuenta `sa`.
4. `api` — contenedor `Modulo5.Api`, publica el puerto 8080 interno como 5080 en el host.
5. `web` — contenedor `Modulo5.Web`, publica el puerto 8080 interno como 5000 en el host, habla con
   `api` por la red interna de Compose (`http://api:8080/`).
6. `.env` / `.env.example` — mecanismo de externalización de secretos de desarrollo
   (`SA_PASSWORD`, `JWT_SIGNING_KEY`), `.env` gitignoreado.
7. Imágenes base públicas: `mcr.microsoft.com/dotnet/sdk:8.0`, `mcr.microsoft.com/dotnet/aspnet:8.0`,
   `mcr.microsoft.com/mssql/server:2017-latest`.
8. `.dockerignore` — gobierna qué entra al contexto de build de las imágenes `api`/`web`.

## Trust boundaries

- **Host ↔ contenedores publicados:** `db` (1433), `api` (5080), `web` (5000) cruzan del host a la
  red de Docker. Es el límite de mayor exposición: cualquier proceso que alcance esos puertos en el
  host cruza hacia dentro del stack.
- **`web` ↔ `api` (red interna de Compose):** confiado — tráfico dentro de la red bridge que crea
  Compose, no accesible desde fuera de los contenedores salvo por los puertos publicados.
- **`db-init`/`api` ↔ `db` (red interna de Compose):** confiado, mismo argumento.
- **Filesystem del host ↔ imagen (build time):** gobernado por `.dockerignore`. Cruzar este límite
  sin las exclusiones correctas mete archivos del host (potencialmente con secretos) dentro de una
  capa de imagen.
- **Desarrollador ↔ `.env` (host):** el archivo `.env` vive en el filesystem local del
  desarrollador, fuera de git; los contenedores lo leen vía `env_file` de Compose.
- **Internet ↔ registry de imágenes:** `docker compose build`/`pull` trae capas desde
  `mcr.microsoft.com`, cadena de suministro fuera del control del repo.

## Riesgos (STRIDE)

| # | Riesgo | STRIDE | Likelihood | Impact | Mitigación |
|---|--------|--------|------------|--------|------------|
| 1 | Puerto 1433 de `db` publicado en `0.0.0.0` (todas las interfaces) expone la cuenta `sa` a cualquier host en la misma red del desarrollador, no solo a `localhost`. | Information Disclosure / Elevation of Privilege | Medium | High | **Mitigado en el spec:** bindear el puerto a `127.0.0.1:1433:1433` en `docker-compose.yml`, restringiendo el acceso a la propia máquina. |
| 2 | `SA_PASSWORD` y `Jwt__SigningKey` hardcodeados en texto plano en `docker-compose.yml` quedan permanentemente en el historial de git una vez commiteado el archivo. | Information Disclosure | High | Medium | **Mitigado en el spec:** externalizar ambos valores a un archivo `.env` (gitignoreado), referenciado desde `docker-compose.yml` vía `env_file`/interpolación `${VAR}`, con un `.env.example` commiteado que documenta las claves con valores placeholder. |
| 3 | Imágenes base con tag flotante (`aspnet:8.0`, `sdk:8.0`, `mssql/server:2017-latest`) sin pin por digest: el contenido de la imagen puede cambiar entre builds sin que el repo lo refleje. | Tampering (cadena de suministro) | Low | Medium | **Riesgo aceptado** (ver abajo). |
| 4 | `db-init` y `api` usan la cuenta `sa` (administrador) de SQL Server en vez de una cuenta de mínimo privilegio. | Elevation of Privilege | Low | Medium | **Riesgo aceptado** (ver abajo), reforzado por la mitigación del riesgo #1 (el puerto ya no es alcanzable desde otras máquinas). |
| 5 | Tráfico SQL entre `api` y `db` sin TLS (`Encrypt=False`, `TrustServerCertificate=True`) dentro de la red interna de Compose. | Information Disclosure | Low | Low | Sin mitigación adicional — tráfico confinado a la red bridge interna de Docker en la propia máquina del desarrollador; TLS/HTTPS está explícitamente Out of Scope en el PRD (no es un entorno accesible desde red externa). |
| 6 | `.dockerignore` incompleto podría filtrar archivos con secretos locales (`*.user`, `appsettings.*.local.json`) al contexto de build. | Information Disclosure | Low | Medium | **Ya mitigado en el diseño:** Block 3 del spec agrega ambos patrones a `.dockerignore` (ver PLAN — decisión ya tomada antes de este reporte). |

## Datos sensibles (F-TM-05)

- **Credenciales:** contraseña de SQL Server (`SA_PASSWORD`) y clave de firma JWT (`Jwt__SigningKey`)
  — exclusivas de este entorno Docker de desarrollo/prueba local, nunca las mismas que un entorno
  real (NFR-02 del PRD). Tras la mitigación del riesgo #2, viven solo en `.env` (no versionado).
- No hay PII ni datos financieros involucrados en este ticket: `docker/seed/002-seed-admin.sql`
  siembra un único usuario administrador de bootstrap con contraseña de desarrollo, ya cubierto por
  el threat model de FEAT-001a.

## Riesgos aceptados (F-TM-04)

**Riesgo #3 — tags flotantes de imagen base:**
- **Quién lo acepta:** Mariano Aranda (usuario del proyecto), confirmado durante PLAN de FEAT-002.
- **Justificación:** el PRD excluye explícitamente despliegue a producción (Out of Scope); pinear
  por digest agrega mantenimiento (actualizar el hash en cada parche de seguridad de la imagen base)
  sin beneficio para un entorno de desarrollo/prueba manual local.
- **Condición de revisión:** si este `docker-compose.yml` se usa alguna vez fuera de
  desarrollo/prueba local (staging, CI, cualquier entorno compartido), revisar y pinear por digest.

**Riesgo #4 — cuenta `sa` en `db-init`/`api`:**
- **Quién lo acepta:** Mariano Aranda (usuario del proyecto), confirmado durante PLAN de FEAT-002.
- **Justificación:** el PRD excluye producción; crear y mantener un rol de mínimo privilegio para un
  contenedor efímero de desarrollo local no aporta una reducción de riesgo proporcional al esfuerzo,
  especialmente ya con el riesgo #1 mitigado (el puerto ya no es alcanzable desde otras máquinas).
- **Condición de revisión:** igual que el riesgo #3 — si el compose se usa fuera de
  desarrollo/prueba local.

## Mitigaciones a incorporar al spec

1. `docker-compose.yml`: bindear el puerto de `db` a `127.0.0.1:1433:1433` (riesgo #1).
2. `docker-compose.yml` + nuevo `.env.example` + `.gitignore`: externalizar `SA_PASSWORD` y
   `Jwt__SigningKey` a variables de entorno leídas de un `.env` local gitignoreado (riesgo #2).
3. `.dockerignore`: agregar `*.user` y `appsettings.*.local.json` (riesgo #6, ya decidido en PLAN).

---

Risks: C:0 H:0 M:2 (aceptados) L:1 (sin mitigación adicional necesaria)
Result: PASSED — mitigaciones folded into spec, riesgos MEDIUM restantes formalmente aceptados con
las tres condiciones de F-TM-04 (quién, justificación, revisión).
