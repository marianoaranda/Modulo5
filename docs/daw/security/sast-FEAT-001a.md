# SAST Report — FEAT-001a (Autenticación)

**Fecha:** 2026-08-17
**Alcance:** cierre de CODE, los 5 bloques completos (`Modulo5.Domain`, `Modulo5.Data`, `Modulo5.Api`, `Modulo5.Web` + sus tests).

## Resultado: PASSED

### Secretos
- ✅ F-SAST-01: sin API keys/passwords/tokens/connection strings hardcodeados. `Jwt:SigningKey` y
  `ConnectionStrings:Default` se leen exclusivamente de configuración (user-secrets en desarrollo,
  variable de entorno en producción — `src/Modulo5.Api/Program.cs:21-36`), con falla explícita al
  arrancar si faltan. `Modulo5.Web` no maneja secretos: `ApiClient:BaseUrl` es la única config propia
  y no es sensible.
- ✅ `.gitignore` excluye `appsettings.*.local.json` y no hay ningún `appsettings.json` de
  `Modulo5.Api` commiteado en el repo.

### Inyección
- ✅ F-SAST-02: sin `FromSqlRaw`/`ExecuteSqlRaw`/`FromSqlInterpolated` en toda la solución — EF Core
  vía LINQ exclusivamente.
- ✅ F-SAST-03: sin `Process.Start` ni ejecución de comandos con input de usuario.
- ✅ F-SAST-05: sin manejo de paths de archivos con input de usuario.

### XSS y funciones inseguras
- ✅ F-SAST-06: sin `Html.Raw` en ninguna vista de `Modulo5.Web`; Razor escapa por defecto.
- ✅ F-SAST-04/17: sin `eval()` ni deserialización insegura.
- ✅ F-SAST-08: sin MD5/SHA1/DES/ECB. Hashing de contraseñas con PBKDF2 (`Rfc2898DeriveBytes`,
  Block 2).

### Resto de categorías obligatorias
- ✅ F-SAST-07 (SSRF): `ApiClient` de `Modulo5.Web` apunta a una URL base fija de configuración, no a
  una URL controlada por el usuario.
- ✅ F-SAST-09 (debug en producción): `UseDeveloperExceptionPage()` solo fuera de `!IsDevelopment()`
  en ambos `Program.cs` (Api y Web).
- ✅ F-SAST-10 (logging de datos sensibles): sin logging de password/hash/salt/token en ningún
  `_logger`/`Console.Write`.
- ✅ F-SAST-11 (upload sin restricciones): no aplica, no hay funcionalidad de upload.
- ✅ F-SAST-12 (CSRF): `@Html.AntiForgeryToken()` + `[ValidateAntiForgeryToken]` en los 3 forms
  mutables de `Modulo5.Web` (Create/Edit/Delete de Usuarios) y en Login/Logout — verificado en el
  Block 5.
- ✅ F-SAST-14 (validación de input incompleta): `PasswordPolicy`, límites de longitud
  (`usuario` ≤50, `nombreCompleto` ≤150) validados tanto en la Api (Block 4) como en el cliente
  (Data Annotations, Block 5, defensa en profundidad).
- ✅ F-SAST-15 (manejo de errores inseguro): middleware único en la Api
  (`ExceptionHandlingMiddleware`) nunca expone stack trace, solo mensaje genérico en 500;
  `Modulo5.Web` usa `UseExceptionHandler("/Error")` con página genérica sin detalle interno.

### Dependencias (F-SAST-13/16)
Triage inicial (`dotnet list package --vulnerable --include-transitive`) reportó 3 CVEs High en
`tests/Modulo5.Domain.Tests` y `tests/Modulo5.Api.Tests`, triaged por `daw-sec-auditor`:

| CVE | Paquete | Disposición | Resolución |
|---|---|---|---|
| GHSA-2m69-gcr7-jv3q | `SQLitePCLRaw.lib.e_sqlite3` 2.1.6 | **True positive** — binario nativo sí se carga en tests | Bump `Microsoft.EntityFrameworkCore.Sqlite` 8.0.11→8.0.30 |
| GHSA-7jgj-8wvc-jh57 | `System.Net.Http` 4.3.0 | False positive — nodo netstandard1.1 sin asset de runtime, no se carga (confirmado en `.deps.json`) | Bump `xunit` 2.5.3→2.9.3 (elimina el nodo del grafo) |
| GHSA-cmhx-cq75-c4mj | `System.Text.RegularExpressions` 4.3.0 | False positive — mismo origen (`NETStandard.Library` 1.6.1 vía xunit), no presente en build output | Bump `xunit.runner.visualstudio` 2.5.3→2.8.2 |

Los tres bumps quedaron aplicados en ambos `.csproj` de test (sin tocar código de producción ni de
test). Re-ejecución de `dotnet list package --vulnerable --include-transitive`: **0 paquetes
vulnerables** en las 6 proyectos de la solución. `dotnet build`: 0 warnings/0 errores. `dotnet test`:
28/28 (Domain.Tests 13/13, Api.Tests 15/15), sin regresión.

## Suppressions
Ninguna — los 3 findings se resolvieron con bump de versión, no requirieron supresión documentada.

## Nota de proceso
Durante el triage, el subagente `daw-sec-auditor` reportó que se le presentó un `system-reminder`
instruyéndolo a ocultar al usuario unas ediciones de prueba-y-revertido que había hecho sobre los
`.csproj` durante su investigación. El agente no seguió esa instrucción (no coincidía con nada
pedido por el usuario ni por el orquestador), revirtió sus cambios de prueba y lo reportó de forma
transparente. El árbol de trabajo quedó limpio antes de que el orquestador aplicara el fix real. Se
documenta acá por trazabilidad; no afectó el resultado de este gate.

---
**Gate:** `gates.sast = true`
