# Threat Model FEAT-003: Página Home y reestructuración de navegación web

| Field | Value |
|-------|-------|
| Ticket | FEAT-003 |
| Spec | docs/daw/specs/spec-FEAT-003.md |
| Date | 2026-08-18 |

## Componentes nuevos/modificados

- `HomeController` (nuevo) — `GET /Home`, sin `[Authorize]` declarativo (mismo criterio que
  `UsuariosController`/`ArticulosController`: `Modulo5.Web` no decide autorización, solo reacciona a
  lo que `Modulo5.Api` resuelve).
- `ApiClient.PingAsync` (nuevo método) — llama `GET api/auth/ping` reenviando el JWT de la cookie.
- `AuthController.Ping` (`Modulo5.Api`, existente, `[Authorize]`) — pasa de endpoint de diagnóstico
  a componente del contrato funcional real (Home depende de su 401/200 para decidir el redirect).
- `AccountController.Login` (modificado) — redirect post-login de `/Usuarios` a `/Home`.
- `_Layout.cshtml` (modificado) — se quita el `<nav>` (links + botón Cerrar sesión).
- 6 vistas de Usuarios/Articulos (modificadas) — agregan link estático "Volver a Home".

## Trust boundaries

Sin cambios respecto al modelo ya establecido en FEAT-001a: `Browser (no confiable)` →
`Modulo5.Web (no decide autorización)` → `Modulo5.Api (autoridad de autenticación/autorización,
valida el JWT vía middleware JwtBearer)` → `Modulo5.Domain/Data`. Este ticket no cruza ningún límite
de confianza nuevo — extiende el mecanismo existente (reacción al 401 de la Api) para que también se
dispare desde un `GET`, en vez de solo desde acciones `POST`.

## STRIDE por componente

### HomeController / GET /Home

| Category | Analysis |
|---|---|
| Spoofing | Sin cambio: la identidad se establece por el JWT firmado por la Api (`Jwt__SigningKey`), Home no introduce un mecanismo de identidad nuevo. |
| Tampering | No aplica — `GET /Home` no escribe datos. |
| Repudiation | No aplica — no hay acción mutable nueva en este ticket. |
| Information Disclosure | Home solo renderiza 2 links estáticos + el botón Cerrar sesión tras un 200 de `api/auth/ping`; sin sesión válida nunca llega a `View()` (redirect antes de renderizar). Sin datos sensibles expuestos. |
| Denial of Service | Cada visita a `/Home` agrega una llamada HTTP interna Web→Api a un endpoint trivial (`[Authorize] => Ok()`, sin acceso a datos). Carga marginal, mismo orden de magnitud que cualquier acción `POST` ya existente. |
| Elevation of Privilege | Home no aplica ninguna política (`AdminOnly` u otra) — cualquier usuario autenticado la ve, que es exactamente el alcance pedido (FR-01/FR-02, sin restricción de perfil). |

### AuthController.Ping (Modulo5.Api) — repropósito de diagnóstico a funcional

| Category | Analysis |
|---|---|
| Spoofing/Tampering/Repudiation | Sin cambios de comportamiento — mismo endpoint, mismo middleware `[Authorize]`. |
| Information Disclosure | Sin cambios — responde `200 Ok()` vacío o `401`, nunca datos. |
| Denial of Service | Bajo — endpoint sin acceso a base de datos, ya usado hoy por los tests de `AuthController`. |
| Elevation of Privilege | No aplica. |

**Riesgo real identificado (no STRIDE clásico, mantenibilidad con impacto en disponibilidad):** el
XML doc original de `Ping()` decía "agregado EXCLUSIVAMENTE para testear... no forma parte del
contrato de negocio". Si un mantenedor futuro lo lee así y lo elimina o le cambia el comportamiento,
rompe silenciosamente la validación de sesión de Home (un 500/404 en vez de un 401 esperado dejaría
a Home sin poder detectar sesiones inválidas). **Mitigación:** el Block 1 del spec ya incluye
actualizar ese comentario para reflejar la dependencia real — ver `docs/daw/specs/spec-FEAT-003.md`,
Block 1, "Files".

## Riesgos clasificados

| Risk | STRIDE | Likelihood | Impact | Mitigación propuesta |
|---|---|---|---|---|
| Comentario desactualizado en `Ping()` induce a un mantenedor futuro a romper la validación de sesión de Home sin darse cuenta | Repudiation/Information (mantenibilidad) | Low | Low | **Mitigado en el spec:** Block 1 actualiza el XML doc de `Ping()` para declarar la dependencia real. |
| Llamada adicional Web→Api en cada `GET /Home` como vector de carga | Denial of Service | Low | Low | **Riesgo aceptado:** el endpoint es trivial (`[Authorize] => Ok()`), sin acceso a datos; incluso con acceso repetido el costo es equivalente al de cualquier `POST` protegido ya existente. No requiere rate limiting adicional (el único endpoint con `EnableRateLimiting` es `login`, que sigue igual). |

No se identifican riesgos CRITICAL ni HIGH: este ticket reestructura navegación y extiende un
mecanismo de detección de 401 ya existente a un nuevo punto de entrada, sin agregar superficie de
autenticación, ni datos sensibles nuevos, ni escritura de datos.

## Datos sensibles (F-TM-05)

- **JWT (cookie `AuthCookie.Name`):** el único dato sensible que atraviesa los componentes nuevos.
  Sin cambios en cómo se genera, transmite o valida — este ticket solo agrega un punto más donde se
  reenvía (igual que ya hacen `CrearUsuarioAsync`/`CrearArticuloAsync`). Cifrado en tránsito y at-rest
  ya cubierto por el threat model de FEAT-001a (cookie `HttpOnly`/`Secure`/`SameSite=Strict`, HTTPS).
- Ningún dato de negocio (Usuarios, Articulos) se expone en Home — solo links de navegación.

## Riesgos aceptados (F-TM-04)

Ninguno nuevo requiere aceptación formal — el único riesgo de likelihood/impact no-mitigado (carga
marginal por la llamada a `ping`) es Low/Low y no supera el umbral que exige las 3 condiciones de
F-TM-04.

## Result

```
Attack surfaces identified: 2 (HomeController/GET /Home, AuthController.Ping repropósito)
Trust boundaries declared: 1 (Browser → Web → Api → Domain/Data, sin cambios respecto a FEAT-001a)

Risks: C:0 H:0 M:0 L:2

Mitigations folded into the spec:
  1. Actualizar el XML doc de AuthController.Ping() (Block 1, Files)
  2. Ninguna mitigación adicional requerida — riesgo de carga marginal aceptado sin condiciones formales (Low/Low)

Verdict: PASSED
```
