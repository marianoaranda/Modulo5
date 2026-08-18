# /daw-verify-module — FEAT-002 (Soporte Docker: Dockerfiles + docker-compose)

**Resultado: PASSED** — 0 FAILs, 0 WARNs bloqueantes (20 checks PASS)
Verificado por: agente `daw-module-verifier` (independiente, no escribió el código)

## Trazabilidad PRD → Spec → Código

| Requisito | Implementación | Evidencia |
|---|---|---|
| FR-01 | `src/Modulo5.Api/Dockerfile` (multi-stage sdk:8.0 → aspnet:8.0) | `docker build` real, exitoso, 0 archivos `.cs` en la imagen runtime |
| FR-02 | `src/Modulo5.Web/Dockerfile` (mismo patrón, solo copia Web) | `docker build` real, exitoso, 0 archivos `.cs` en la imagen runtime |
| FR-03 | `docker-compose.yml` — 4 servicios `db`/`db-init`/`api`/`web` | `docker compose config` + `docker compose up` reales |
| FR-04 | `docker-compose.yml:35-40` (entrypoint de `db-init`) | Logs reales: `CREATE DATABASE` + `001-migrations.sql` + `002-seed-admin.sql`, en ese orden |
| FR-05 | `api` con `depends_on: db-init: condition: service_completed_successfully` | `api` arrancó recién tras que `db-init` saliera `Exited (0)` |
| FR-06 | `web` → `http://api:8080/` por la red interna de Compose | Login end-to-end real: Web autenticó contra Api, JWT emitido, `/Usuarios` cargó 200 |
| FR-07 | `.dockerignore` (`**/*.user`, `**/appsettings.*.local.json`) | Archivo de prueba en `src/Modulo5.Api/` confirmado ausente del contexto de build y de la imagen |
| FR-08 | Puertos host: `api` → `5080:8080`, `web` → `5000:8080` | `curl` real a ambos puertos desde el host |
| NFR-01 | Volumen nombrado `modulo5-db-data` | `docker-compose.yml:77` |
| NFR-02 | `.env` gitignoreado + `.env.example` con placeholders + comentarios explícitos | `git check-ignore -v .env` confirma exclusión; `.env` nunca aparece en `git status` |
| NFR-03 | Cadena `depends_on`/`healthcheck` (`db` healthy → `db-init` completo → `api` → `web`) | Orden real observado en `docker compose up`, sin intervención manual |

## Criterios de aceptación (AC-01 a AC-06)

- ✅ AC-01 — orden de arranque `db → db-init → api → web` confirmado con `docker compose up` real.
- ✅ AC-02 — `db-init` crea la base y aplica ambos scripts de seed en orden (logs reales).
- ✅ AC-03 — `depends_on: condition: service_healthy` (mecanismo nativo de Compose), ejercitado indirectamente: `db-init` solo arrancó tras `db` en estado `healthy`.
- ✅ AC-04 — `http://localhost:5080/` responde desde el host (Kestrel vivo).
- ✅ AC-05 — login real Web→Api por la red interna, extremo a extremo (JWT emitido, `/Usuarios` 200).
- ✅ AC-06 — confirmado con build real (ver FR-07).

## Bloques del spec

- ✅ Block 1 (Dockerfiles) — coincide con las `ProjectReference` reales de Api (Domain+Data) y Web (ninguna).
- ✅ Block 2 (compose + credenciales) — puerto de `db` en `127.0.0.1:1433`, secretos vía `${VAR:?...}`, `.env.example` + `.gitignore` correctos.
- ✅ Block 3 (`.dockerignore`) — incluye `**/*.user` y `**/appsettings.*.local.json` además de lo heredado.
- Sin scope creep: ningún archivo ni servicio fuera de lo pedido por spec/PRD.

## Threat model — 6 riesgos

- ✅ #1 (puerto 1433 expuesto) → mitigado: `127.0.0.1:1433:1433`.
- ✅ #2 (secretos hardcodeados) → mitigado: `${SA_PASSWORD:?...}`, `${JWT_SIGNING_KEY:?...}`, `.env` gitignoreado, `.env.example` con placeholders.
- ✅ #3 (tags flotantes de imagen base) → riesgo aceptado formalmente (quién/justificación/condición de revisión presentes en `threat-FEAT-002.md`).
- ✅ #4 (cuenta `sa` de privilegio elevado) → riesgo aceptado formalmente, mismas 3 condiciones.
- ✅ #5 (tráfico SQL sin TLS interno) → sin mitigación adicional, justificado (tráfico confinado a la red bridge local de Compose, TLS fuera de alcance).
- ✅ #6 (`.dockerignore` incompleto) → mitigado, confirmado empíricamente.

## Gates previos

- ✅ SAST (`sast-FEAT-002.md`): 12 clean, 0 vulnerabilidades — PASSED.
- ✅ Threat model: PASSED, riesgos Medium aceptados con las 3 condiciones de F-TM-04.
- ✅ Tests de cierre de CODE: 28/28 (13 Domain + 15 Api) + evidencia real de los 7 tests de infraestructura del Block 2 y 1 del Block 3.

## Verificación empírica adicional en este gate

- `docker compose up` real del stack completo (con el puerto host de `db` remapeado temporalmente en una copia local para no chocar con un contenedor ajeno de otro módulo que ya ocupaba el 1433 en esta máquina — el `docker-compose.yml` versionado no se tocó).
- Login end-to-end Web → Api → Db con el usuario `admin` sembrado.
- Build real de ambas imágenes + test empírico de `.dockerignore`.
- Limpieza completa (`docker compose down -v`, imágenes de prueba eliminadas); `git status --short` sin cambios respecto al estado ya commiteado.

## Veredicto

**PASSED** — 11 requisitos (8 FR + 3 NFR) ✅ · 6 ACs ✅ · 3 bloques ✅ · 6 riesgos del threat model mitigados/aceptados ✅ · 0 FAILs.

Sin recomendaciones bloqueantes antes de RELEASE.
