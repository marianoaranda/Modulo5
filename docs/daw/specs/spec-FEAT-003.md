# Spec FEAT-003: Página Home y reestructuración de navegación web

| Field | Value |
|-------|-------|
| Ticket | FEAT-003 |
| PRD | docs/daw/prd/prd-FEAT-003.md |
| Tier | FEATURE |
| Date | 2026-08-18 |
| Spec loops | 0 |

## Summary

Se agrega `HomeController` + `Views/Home/Index.cshtml`, punto de entrada post-login con el menú
completo (Usuarios, Articulos, Cerrar sesión). El `GET` de Home fuerza la validación del JWT
llamando a `GET /api/auth/ping` (endpoint `[Authorize]` ya existente en la Api, agregado en
FEAT-001a para testear el middleware JwtBearer — se reutiliza acá con el mismo propósito: forzar el
401 sin agregar un mecanismo de auth nuevo del lado Web). `AccountController.Login` cambia su
redirect post-login de `/Usuarios` a `/Home`. El `<nav>` se saca de `_Layout.cshtml` (que además
resultó no aplicar a `Login.cshtml`, que ya tiene `Layout = null` propio) y cada vista de
Usuarios/Articulos (Index/Create/Edit) suma un link "Volver a Home" — el link "Cancelar" que ya
tienen Create/Edit se mantiene sin cambios, conviven.

## Coverage: PRD → blocks

| Requirement | Covered by |
|---|---|
| FR-01 | Block 1 |
| FR-02 | Block 1 |
| FR-03 | Block 2 |
| FR-04 | Block 3 |
| FR-05 | Block 3 |
| NFR-01 | Strategy: Block 1 reutiliza `GET /api/auth/ping` ([Authorize] ya existente) vía el mismo patrón `SendAuthenticatedAsync`/`HandleUnauthorizedOrForbidden` que ya usan Usuarios/Articulos — mismo mecanismo de 401, disparado también desde un GET |

## Dependencies between blocks

Block 2 depende de que Block 1 exista (`AccountController` redirige a un `HomeController` que tiene
que estar ya creado para no romper el login). Block 3 es independiente de los otros dos (solo toca
`_Layout.cshtml` y las vistas de Usuarios/Articulos) pero se hace último para no dejar a la app sin
ningún menú visible mientras Home todavía no existe. Orden: **1 → 2 → 3**.

## Block 1 — HomeController + página Home

**Files**
- `src/Modulo5.Web/Controllers/HomeController.cs` (new)
- `src/Modulo5.Web/Views/Home/Index.cshtml` (new)
- `src/Modulo5.Web/Services/ApiClient.cs` (modified) — agrega `PingAsync`
- `src/Modulo5.Api/Controllers/AuthController.cs` (modified) — actualiza el comentario XML de
  `Ping()`: hoy dice "agregado EXCLUSIVAMENTE para poder testear el middleware JwtBearer... no forma
  parte del contrato de negocio", lo cual pasa a ser falso una vez que `Home` depende de este
  endpoint para validar sesión. Se corrige el comentario para reflejar que ahora es parte del
  contrato funcional (usado por `Modulo5.Web` para forzar la validación del JWT en `GET /Home`). Sin
  cambios de comportamiento ni de firma — no es un cambio de la Api en el sentido del PRD ("Out of
  Scope: no hay cambios en la Api"), es corregir documentación que quedaría desactualizada.

**Logic**
`ApiClient.PingAsync(CancellationToken ct = default)` llama `GET api/auth/ping` vía
`SendAuthenticatedAsync<object?>(HttpMethod.Get, "api/auth/ping", body: null, ct)` — mismo método
privado que ya usan `CrearUsuarioAsync`/`CrearArticuloAsync` (reenvía el JWT de la cookie si existe,
no agrega el header si no existe).

`HomeController` (sin `[Authorize]`, mismo criterio que `UsuariosController`/`ArticulosController`:
`Modulo5.Web` no decide autorización por sí mismo, ver `Program.cs:46-48`) expone
`[Route("Home")]`, `[HttpGet]` `Index()`:
1. Llama `_apiClient.PingAsync(ct)`.
2. Si el `ApiResult` tiene `StatusCode == 401` → `RedirectToAction("Login", "Account")` (replica
   `HandleUnauthorized` de `ArticulosController.cs:148` — no `HandleUnauthorizedOrForbidden` de
   `UsuariosController.cs:124`, que sí maneja 403 porque su endpoint de Api tiene la política
   `AdminOnly`. `api/auth/ping` no tiene política adicional, mismo caso que Articulos: sin rama 403).
3. Si es 200 → `View()`.

`Views/Home/Index.cshtml` muestra: `<a href="/Usuarios">Usuarios</a>`,
`<a href="/Articulos">Articulos</a>`, y el mismo `<form>`/botón "Cerrar sesión" que hoy vive en
`_Layout.cshtml:12-15` (se mueve tal cual, mismo `asp-controller="Account" asp-action="Logout"` +
`@Html.AntiForgeryToken()`).

**Input validation**
No aplica (sin input de usuario en este bloque).

**Error handling**
- 401 de `api/auth/ping` → redirect a Login (arriba).
- Cualquier otro código de error, o `HttpRequestException` si la Api no está disponible → burbujea
  al middleware `UseExceptionHandler("/Error")` existente, mismo criterio que el resto de `ApiClient`
  (no se atrapa acá a propósito).

**Required tests**
*(No hay `Modulo5.Web.Tests`, mismo criterio que FEAT-001a/b/FIX-001 — verificación manual en CODE)*
- [ ] Con sesión válida (cookie JWT presente), `GET /Home` responde 200 y la vista muestra los 3
      elementos (Usuarios, Articulos, Cerrar sesión) — valida AC-02.
- [ ] Sin sesión (sin cookie, o cookie vencida/inválida), `GET /Home` redirige a `/Account/Login` —
      valida AC-05.
- [ ] Desde Home, clic en "Cerrar sesión" cierra la sesión y redirige a Login — valida AC-06.
- [ ] Con la Api caída/inalcanzable, `GET /Home` no muestra una excepción no controlada — cae en la
      página de error genérica (`/Error`), mismo criterio que el test manual equivalente de
      FEAT-001a Block 5 — valida el manejo de `HttpRequestException` documentado arriba.

**Completion criterion**
`GET /Home` con sesión válida muestra el menú completo; sin sesión válida redirige a Login sin
excepciones no controladas.

## Block 2 — Redirect post-login a Home

**Files**
- `src/Modulo5.Web/Controllers/AccountController.cs` (modified)

**Logic**
En el `POST Login` exitoso (línea actual `return RedirectToAction("Index", "Usuarios");`), cambiar
el target a `return RedirectToAction("Index", "Home");`.

**Input validation**
No aplica (bloque no toca el formulario de Login).

**Error handling**
Sin cambios — el manejo de credenciales inválidas (spec Block 5 de FEAT-001a) sigue igual.

**Required tests**
- [ ] Login con credenciales válidas redirige a `/Home` (no a `/Usuarios`) — valida AC-01.

**Completion criterion**
Un login exitoso deja al usuario en `/Home`, no en `/Usuarios`.

## Block 3 — Quitar el menú de Usuarios/Articulos, agregar "Volver a Home"

**Files**
- `src/Modulo5.Web/Views/Shared/_Layout.cshtml` (modified) — quita el `<nav>` completo
- `src/Modulo5.Web/Views/Usuarios/Index.cshtml` (modified)
- `src/Modulo5.Web/Views/Usuarios/Create.cshtml` (modified)
- `src/Modulo5.Web/Views/Usuarios/Edit.cshtml` (modified)
- `src/Modulo5.Web/Views/Articulos/Index.cshtml` (modified)
- `src/Modulo5.Web/Views/Articulos/Create.cshtml` (modified)
- `src/Modulo5.Web/Views/Articulos/Edit.cshtml` (modified)

**Logic**
`_Layout.cshtml`: se quita el `<header><nav>...</nav></header>` completo (líneas 9-17 actuales,
incluido el botón Cerrar sesión que ahora vive solo en Home). El resto de `_Layout.cshtml` (título,
bloques de `TempData["Mensaje"]`/`TempData["Error"]`, `@RenderBody()`) queda igual — sigue siendo el
layout de Usuarios, Articulos, Error y AccesoDenegado (Login ya tiene `Layout = null` propio, no le
afecta este cambio).

Cada una de las 6 vistas de Usuarios/Articulos agrega, al principio del contenido (antes del
`<h1>` existente): `<p><a href="/Home">Volver a Home</a></p>`. El link "Cancelar" que ya existe en
las 4 vistas Create/Edit (apunta a su propio `/Usuarios` o `/Articulos`) NO se toca — convive con el
nuevo link a Home, son dos acciones distintas (cancelar el form vs. salir de la sección).

**Input validation**
No aplica (solo markup estático).

**Error handling**
No aplica.

**Required tests**
- [ ] Ninguna vista de Usuarios (Index/Create/Edit) muestra el link a Articulos ni el botón Cerrar
      sesión — valida AC-03.
- [ ] Ninguna vista de Articulos (Index/Create/Edit) muestra el link a Usuarios ni el botón Cerrar
      sesión — valida AC-04.
- [ ] Las 6 vistas muestran "Volver a Home" y navega correctamente a `/Home`.
- [ ] Las 4 vistas Create/Edit siguen mostrando "Cancelar" sin cambios.

**Completion criterion**
Ninguna vista de Usuarios/Articulos muestra el menú de navegación; todas tienen "Volver a Home"
funcional.

## Final verification

- Login exitoso → `/Home`, con el menú completo (Usuarios, Articulos, Cerrar sesión).
- `GET /Home` sin sesión válida → redirect a Login (mismo mecanismo de 401 que ya usan
  Create/Edit de Usuarios/Articulos, ahora también en un GET).
- Usuarios e Articulos (Index/Create/Edit) no muestran el menú, solo "Volver a Home" (+ "Cancelar"
  sin cambios en Create/Edit).
- Cerrar sesión desde Home sigue funcionando igual que antes (mismo `AccountController.Logout`).
- `Views/Error/Index.cshtml` y `Views/Shared/AccesoDenegado.cshtml` quedan sin el `<nav>` (por la
  herencia de `_Layout.cshtml`) pero conservan sus propios links existentes hacia `/Usuarios`
  ("Volver al inicio"/"Volver") — inconsistencia aceptada, Out of Scope del PRD.
