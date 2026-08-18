# /daw-verify-module — FEAT-003 (Página Home y reestructuración de navegación web)

**Resultado: PASSED** — 0 FAILs, 0 WARNs (21 checks PASS)
Verificado por: agente `daw-module-verifier` (independiente, no escribió el código)

## Trazabilidad PRD → Código → Evidencia

| Requisito | Implementación | Evidencia |
|---|---|---|
| FR-01 | `HomeController.Index()` llama `ApiClient.PingAsync` → `GET api/auth/ping` (`[Authorize]`) | GET /Home sin cookie → 302 a `/Account/Login` (real, end-to-end) |
| FR-02 | `Views/Home/Index.cshtml` (links Usuarios/Articulos + form Logout) | GET /Home autenticado → 200, HTML real con los 3 elementos |
| FR-03 | `AccountController.Login` (POST), redirect a `"Home"` | Login válido → `302 Location: /Home` |
| FR-04 | `Usuarios/Index,Create,Edit.cshtml` — "Volver a Home", sin menú | Confirmado en vivo en las 3 vistas |
| FR-05 | `Articulos/Index,Create,Edit.cshtml` — ídem | Confirmado en vivo en las 3 vistas |
| NFR-01 | `HomeController.HandleUnauthorized` replica `ArticulosController.HandleUnauthorized` — sin validar JWT localmente en Web | Código idéntico en estructura, sin mecanismo nuevo |

## Criterios de aceptación (AC-01 a AC-06)

- ✅ AC-01 — POST Login válido → `302 Location: /Home`.
- ✅ AC-02 — GET /Home autenticado → 200, HTML con Usuarios, Articulos y Cerrar sesión.
- ✅ AC-03 — Usuarios (Index/Create/Edit) → solo "Volver a Home" (+ "Cancelar" intacto en Create/Edit), sin menú ni logout.
- ✅ AC-04 — Articulos (Index/Create/Edit) → ídem.
- ✅ AC-05 — GET /Home sin sesión → `302` a `/Account/Login`.
- ✅ AC-06 — Logout desde Home → cookie borrada + `302` a Login; una visita posterior a `/Home` con la misma cookie vuelve a redirigir (sesión efectivamente cerrada).

## Bloques del spec

- ✅ Block 1 (HomeController + página Home) — completo, incluye el comentario actualizado de `AuthController.Ping()`.
- ✅ Block 2 (redirect post-login a Home) — 1/1 línea, completo.
- ✅ Block 3 (quitar menú de Usuarios/Articulos, agregar "Volver a Home") — 7/7 archivos, completo.

## Threat model — mitigaciones

- ✅ Riesgo 1 (comentario desactualizado de `Ping()`) → mitigado: XML doc actualizado, sin cambio de firma/comportamiento/política de autorización.
- ✅ Riesgo 2 (carga marginal por la llamada a `ping` en cada `GET /Home`) → aceptado explícitamente sin condiciones formales (Low/Low, no supera el umbral de F-TM-04).

## Loops correctivos del PRD (2)

- ✅ Loop 1 (NFR-01/AC-05 contradictorios, detectado por el impact scan en PLAN) → PRD corregido (commit `2c629ae`): FR-01/NFR-01 ahora reflejan que Home sí llama a la Api desde el `GET`, consistente con AC-05 y con el código final.
- ✅ Loop 2 (Out of Scope "sin cambios en la Api" vs. la necesidad de corregir un comentario en `AuthController.Ping()`, detectado por el arch-auditor en PLAN) → PRD corregido (commit `8ed8577`): excepción puntual para comentarios/documentación en la Api. El único cambio en `Modulo5.Api` es exactamente ese comentario, sin tocar firma/comportamiento/política.

Ambos loops quedan resueltos sin contradicciones remanentes en la versión final del PRD/spec.

## Scope

- ✅ Nada del PRD/spec quedó sin implementar.
- ✅ Sin scope creep — único archivo tocado en `Modulo5.Api` es el comentario de `Ping()` (excepción explícita del PRD).
- ✅ `Views/Error/Index.cshtml` y `Views/Shared/AccesoDenegado.cshtml` no fueron tocadas (Out of Scope explícito del PRD, confirmado sin commits sobre esos archivos).

## Calidad

- ✅ Lint: `dotnet format --verify-no-changes` sin cambios.
- ✅ Tests automatizados: 49/49 passed (25 Domain + 24 Api), sin regresiones por el cambio de comentario en `Ping()`.
- ✅ Sin código muerto, diffs mínimos y coherentes con el spec.
- ℹ️ Sin `Modulo5.Web.Tests` (criterio ya establecido y aceptado desde FEAT-001a/b/FIX-001) — verificación manual end-to-end real (curl + cookies contra el stack Docker) en cada bloque y en este gate.

## Veredicto

**PASSED** — 5 FR + 1 NFR ✅ · 6 ACs ✅ · 3 bloques ✅ · 2 riesgos del threat model mitigados/aceptados ✅ · 2 loops correctivos resueltos ✅ · 0 FAILs.

Sin recomendaciones bloqueantes antes de RELEASE.
