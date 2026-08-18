# SAST Report FEAT-001b: ABM de Artículos

| Field | Value |
|-------|-------|
| Ticket | FEAT-001b |
| Date | 2026-08-18 |
| Scope | Archivos creados/modificados en Blocks 1-3 (Domain, Data, Api, Web) |

## Secrets (F-SAST-01)
✅ Sin patrones de API key/password/token/connection string hardcodeados en los archivos nuevos.
✅ Ningún secreto nuevo introducido — reutiliza `Jwt:SigningKey`/`ConnectionStrings:Default` ya
   gestionados vía `user-secrets`/variables de entorno desde FEAT-001a.

## Injection
✅ F-SAST-02 (SQL/NoSQL): `ArticuloRepository` usa EF Core exclusivamente (LINQ + `SaveChangesAsync`),
   sin `FromSqlRaw`/`ExecuteSqlRaw`/concatenación de SQL.
✅ F-SAST-03 (Command injection): sin `Process.Start` ni invocación de shell con input de usuario.
✅ F-SAST-05 (Path traversal): sin manejo de rutas de archivo con input de usuario en este ticket.

## XSS y funciones inseguras
✅ F-SAST-06 (XSS): sin `innerHTML`/`@Html.Raw`/`dangerouslySetInnerHTML` en
   `Views/Articulos/*.cshtml` — Razor escapa por defecto.
✅ F-SAST-04/17 (deserialización insegura, eval): sin `eval()`, sin `BinaryFormatter`.
✅ F-SAST-08 (crypto débil): no aplica — este ticket no maneja hashing/crypto (hereda PBKDF2 de
   FEAT-001a sin tocarlo).

## Resto de categorías obligatorias
✅ F-SAST-09 (debug mode): sin cambios en configuración de entorno.
✅ F-SAST-10 (logging de datos sensibles): `ArticulosController.LogOperacionExitosa` (Api) solo
   loguea `Codigo`, operación, `UsuarioId` del actor y timestamp — nunca `PrecioCosto`/`Margen` ni
   ningún dato de otro usuario.
✅ F-SAST-11 (upload sin restricciones): no aplica, este ticket no maneja upload de archivos.
✅ F-SAST-12 (CSRF): `[ValidateAntiForgeryToken]` presente en `Create`/`Edit`/`Delete` de
   `Web/Controllers/ArticulosController.cs` (líneas 44, 79, 120); `@Html.AntiForgeryToken()` en los
   3 formularios correspondientes (`Create.cshtml:11`, `Edit.cshtml:18`, `Index.cshtml:34`).
✅ F-SAST-14 (validación de input incompleta): `ArticuloValidationPolicy` (Domain) + validación de
   forma en el controller Api (`codigo`/`descripcion`) + Data Annotations en los ViewModels Web —
   defensa en profundidad en las 3 capas.
✅ F-SAST-15 (error handling que filtra internals): las excepciones no controladas siguen
   traducidas por el middleware existente (`ExceptionHandlingMiddleware`, sin modificar) a un
   mensaje genérico, nunca el stack trace.

## Dependencias (F-SAST-13/16)
✅ Ningún `.csproj` modificado en este ticket — cero dependencias NuGet nuevas, sin superficie de
   cadena de suministro adicional.

## Suppressions
Ninguna — no hubo hallazgos Medium que requieran documentación de excepción.

## Resumen
Total: 15 categorías revisadas, 0 vulnerabilidades (0 Critical, 0 High, 0 Medium sin suprimir).
