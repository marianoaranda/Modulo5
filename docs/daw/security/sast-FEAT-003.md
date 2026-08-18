# SAST FEAT-003: Página Home y reestructuración de navegación web

| Field | Value |
|-------|-------|
| Ticket | FEAT-003 |
| Threat model | docs/daw/security/threat-FEAT-003.md |
| Scope | `HomeController.cs`, `Home/Index.cshtml`, `ApiClient.cs` (diff), `AuthController.cs` (diff — solo comentario), `AccountController.cs` (diff — 1 línea), `_Layout.cshtml` (diff), 6 vistas de Usuarios/Articulos (diff) |
| Date | 2026-08-18 |

## Findings

### Secrets (F-SAST-01)
✅ Ningún secreto en texto plano en ninguno de los archivos del diff. `ApiClient.PingAsync`
reenvía el JWT ya existente de la cookie (mismo mecanismo revisado en el SAST de FEAT-001a), sin
introducir un nuevo valor sensible.

### Injection (F-SAST-02, F-SAST-03, F-SAST-05)
✅ `HomeController` no recibe input de usuario (`GET` sin parámetros de ruta ni query). Los 6
`<a href="/Home">`/`<a href="/Usuarios">`/`<a href="/Articulos">` agregados son strings estáticos,
sin interpolación de datos de usuario — sin superficie de XSS ni de inyección.

### Open redirect
✅ `HomeController.HandleUnauthorized` usa `RedirectToAction("Login", "Account")` — un target fijo
por nombre de controller/acción (resuelto por el framework), no una URL tomada de input externo. Sin
riesgo de open redirect.

### XSS / funciones inseguras / CSRF (F-SAST-06, F-SAST-12)
✅ No aplica — el diff es markup estático y controllers sin renderizado de datos de usuario. El
form de "Cerrar sesión" movido a `Home/Index.cshtml` conserva `@Html.AntiForgeryToken()` idéntico al
original de `_Layout.cshtml`.

### Crypto / debug / dependencias (F-SAST-08, F-SAST-09, F-SAST-13, F-SAST-16)
✅ Sin cambios de crypto, sin dependencias nuevas. `AuthController.Ping()` no cambió comportamiento,
firma ni política de autorización — solo su comentario XML.

### Validación de input / manejo de errores (F-SAST-14, F-SAST-15)
✅ No aplica — sin input nuevo. El manejo de `HttpRequestException` (Api caída) sigue el patrón ya
existente de `ApiClient` (burbujea al middleware `UseExceptionHandler`), verificado con test manual
en Block 1 (ver `docs/daw/reports/verify-FEAT-003.md` cuando se genere en VERIFY).

## Suppressions
Ninguna.

## Result

```
Total: 8 clean, 0 vulnerabilities (0 critical, 0 high, 0 medium sin documentar)
Verdict: PASSED
```
