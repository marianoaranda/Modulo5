# SAST FEAT-002: Soporte Docker (Dockerfiles + docker-compose)

| Field | Value |
|-------|-------|
| Ticket | FEAT-002 |
| Threat model | docs/daw/security/threat-FEAT-002.md |
| Scope | `.dockerignore`, `.env.example`, `.gitignore` (diff), `docker-compose.yml`, `docker/seed/001-migrations.sql`, `docker/seed/002-seed-admin.sql`, `src/Modulo5.Api/Dockerfile`, `src/Modulo5.Web/Dockerfile` |
| Date | 2026-08-18 |

## Findings

### Secrets (F-SAST-01)
✅ `docker-compose.yml` no tiene ningún secreto en texto plano: las 4 referencias a
`SA_PASSWORD`/`Jwt__SigningKey` usan `${VAR:?mensaje}`, resueltas desde un `.env` local.
✅ `.env` está gitignoreado (`git check-ignore -v .env` confirma `.gitignore:24:.env`) y no aparece
en `git status` ni `git ls-files`.
✅ `.env.example` solo contiene placeholders (`changeme-local-only`), sin valores reales.
✅ `docker/seed/002-seed-admin.sql` contiene un hash+salt PBKDF2 (no la contraseña en texto plano)
de un usuario de bootstrap de desarrollo. **No es un finding nuevo**: ya está identificado y
aceptado explícitamente en `threat-FEAT-002.md` ("Datos sensibles", líneas 57-58), que lo remite a
la cobertura ya dada en el threat model de FEAT-001a. No amerita una nueva supresión — es un riesgo
ya evaluado en PLAN.

### Injection (F-SAST-02, F-SAST-03, F-SAST-05)
✅ `docker/seed/001-migrations.sql` y `002-seed-admin.sql` son scripts estáticos, sin concatenación
dinámica ni entrada de usuario.
✅ El entrypoint de `db-init` en `docker-compose.yml` pasa `$$SA_PASSWORD` a `sqlcmd -P`: es una
variable de entorno fijada en tiempo de compose por el desarrollador (no input de un usuario final
ni de una fuente externa), por lo que no aplica como inyección de comandos.
✅ No hay manejo de rutas de archivo con input externo en el diff.

### XSS / funciones inseguras / SSRF / CSRF / upload (F-SAST-06, F-SAST-07, F-SAST-11, F-SAST-12)
✅ No aplica — el diff es infraestructura Docker, sin código de renderizado HTML, endpoints nuevos
ni manejo de archivos subidos.

### Crypto débil (F-SAST-08)
✅ El hash del seed admin usa PBKDF2-HMAC-SHA256 (210.000 iteraciones, salt 16 bytes), consistente
con `Pbkdf2PasswordHasher` del proyecto (AGENTS.md, "Autenticación") — no se introduce crypto nueva
ni débil en este ticket.

### Debug/logging (F-SAST-09, F-SAST-10)
✅ `ASPNETCORE_ENVIRONMENT=Development` en `api`/`web` está acotado a este `docker-compose.yml` de
prueba manual local — el PRD excluye explícitamente despliegue a producción (Out of Scope). No hay
logging nuevo de datos sensibles.

### Validación de input / manejo de errores (F-SAST-14, F-SAST-15)
✅ No aplica — no hay lógica de aplicación nueva en este ticket (solo configuración de
infraestructura).

### Dependencias (F-SAST-13, F-SAST-16)
✅ No se agregó ninguna dependencia NuGet/npm en este ticket. Las imágenes base
(`mcr.microsoft.com/dotnet/sdk:8.0`, `aspnet:8.0`, `mssql/server:2017-latest`) usan tags flotantes,
riesgo ya identificado y **aceptado explícitamente** en `threat-FEAT-002.md` ("Riesgos aceptados",
riesgo #3 — Mariano Aranda, PLAN de FEAT-002, condición de revisión: pinear por digest si se usa
fuera de desarrollo/prueba local).

## Suppressions
Ninguna nueva. Los dos hallazgos de riesgo aceptado/ya cubierto (seed admin, tags flotantes) están
documentados en `threat-FEAT-002.md`, no en este reporte, por ser decisiones de PLAN.

## Result

```
Total: 12 clean, 0 vulnerabilities (0 critical, 0 high, 0 medium sin documentar)
Verdict: PASSED
```
