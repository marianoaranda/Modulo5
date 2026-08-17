# Spec FEAT-001a: Autenticación (Usuarios, Credenciales y Login JWT)

| Field | Value |
|-------|-------|
| Ticket | FEAT-001a |
| PRD | docs/daw/prd/prd-FEAT-001a.md |
| Tier | FEATURE |
| Date | 2026-08-17 |
| Spec loops | 0 |

## Summary

Se construye la base de autenticación del proyecto: una solución .NET 8 en 4 capas (Web/Api/Domain/
Data), con usuarios cuyas contraseñas se guardan hasheadas (PBKDF2+salt), login que emite JWT, y un
ABM de Usuarios protegido por perfil administrador. El perfil "administrador" se precarga por seed
de migración, sin pantalla de ABM de Perfiles. Las mitigaciones del threat model
(`docs/daw/security/threat-FEAT-001a.md`) quedan incorporadas bloque a bloque: secretos por variable
de entorno, rate limiting en login, cookie HttpOnly para el JWT, CSRF, autorización explícita por
perfil, y DTOs que nunca exponen Hash/Salt.

## Coverage: PRD → blocks

| Requirement | Covered by |
|---|---|
| FR-01 | Block 1 (modelo), Block 4 (endpoints), Block 5 (UI) |
| FR-02 | Block 1 (modelo), Block 4 (endpoints), Block 5 (UI) |
| FR-03 | Block 1 (modelo), Block 4 (endpoints), Block 5 (UI) |
| FR-04 | Block 2 |
| FR-05 | Block 2 |
| FR-06 | Block 2 |
| FR-07 | Block 4 |
| FR-08 | Block 3, Block 5 |
| FR-09 | Block 3 |
| FR-10 | Block 1 |
| NFR-01 | Strategy: Block 5 crea `Modulo5.Web` como sitio ASP.NET MVC .NET 8 |
| NFR-02 | Strategy: Block 3 crea `Modulo5.Api` como Web API REST .NET 8 en proyecto aparte, con JWT |
| NFR-03 | Strategy: Block 1 configura EF Core 8 sobre SQL Server 2017 |
| NFR-04 | Strategy: la arquitectura por capas y el pool de conexiones por defecto de EF Core soportan 1-5 usuarios concurrentes sin configuración adicional; no requiere caché ni balanceo |

## Dependencies between blocks

- Block 2 depende de Block 1 (usa la entidad `Usuario` y las excepciones de dominio).
- Block 3 depende de Block 1 y Block 2 (`AuthenticationService` usa `IUsuarioRepository` y
  `PasswordHasher`).
- Block 4 depende de Block 3 (usa el middleware de autenticación JWT y el middleware de manejo de
  errores).
- Block 5 depende de Block 3 y Block 4 (consume `POST /api/auth/login` y `/api/usuarios`).

Orden de ejecución: 1 → 2 → 3 → 4 → 5.

## Block 1 — Solución base, Domain y Data

**Files**
- `Modulo5.sln` (new)
- `.editorconfig` (new) — reglas de formato .NET
- `.gitignore` (modified) — agrega `bin/`, `obj/`, `*.user`, `appsettings.*.local.json`
- `src/Modulo5.Domain/Modulo5.Domain.csproj` (new)
- `src/Modulo5.Domain/Entities/Usuario.cs` (new)
- `src/Modulo5.Domain/Entities/Perfil.cs` (new)
- `src/Modulo5.Domain/Exceptions/ValidationException.cs` (new)
- `src/Modulo5.Domain/Exceptions/NotFoundException.cs` (new)
- `src/Modulo5.Domain/Exceptions/UnauthorizedDomainException.cs` (new)
- `src/Modulo5.Domain/Repositories/IUsuarioRepository.cs` (new)
- `src/Modulo5.Domain/Repositories/IPerfilRepository.cs` (new)
- `src/Modulo5.Data/Modulo5.Data.csproj` (new)
- `src/Modulo5.Data/Modulo5DbContext.cs` (new)
- `src/Modulo5.Data/Configurations/UsuarioConfiguration.cs` (new)
- `src/Modulo5.Data/Configurations/PerfilConfiguration.cs` (new)
- `src/Modulo5.Data/Repositories/UsuarioRepository.cs` (new)
- `src/Modulo5.Data/Repositories/PerfilRepository.cs` (new)
- `src/Modulo5.Data/Migrations/*` (new) — migración inicial con seed del perfil "administrador"
- `tests/Modulo5.Domain.Tests/Modulo5.Domain.Tests.csproj` (new) — esqueleto xUnit
- `tests/Modulo5.Api.Tests/Modulo5.Api.Tests.csproj` (new) — esqueleto xUnit (sin tests todavía,
  referencia a `Modulo5.Api` que se crea en el Block 3)

**Logic**
`Modulo5.Domain` no depende de ningún otro proyecto — define entidades puras y las interfaces que
`Modulo5.Data` implementa. `Modulo5.Data` traduce operaciones de repositorio a EF Core. La migración
inicial de EF Core siembra una única fila en `Perfiles` (`Descripcion = "administrador"`) — no existe
pantalla ni endpoint de ABM de Perfiles (fuera de alcance del PRD). La connection string se lee de
`user-secrets` en desarrollo y de una variable de entorno (`ConnectionStrings__Default`) en
producción; `appsettings.json` NUNCA contiene la connection string real (mitigación de riesgo #1 del
threat model).

**Data model**

`Perfil`
| Field | Type | Constraints |
|---|---|---|
| PerfilId | int | PK, identity |
| Descripcion | nvarchar(100) | not null |

`Usuario`
| Field | Type | Constraints |
|---|---|---|
| UsuarioId | int | PK, identity |
| Usuario | nvarchar(50) | not null, unique |
| NombreCompleto | nvarchar(150) | not null |
| Hash | varbinary(64) | not null |
| Salt | varbinary(16) | not null |
| PerfilId | int | not null, FK → `Perfiles.PerfilId` |

Índice: `IX_Usuario_Usuario` único sobre `Usuario.Usuario` (evita colisión de nombre de usuario, no
exigido explícitamente por el PRD pero necesario para que el login sea determinístico).

**Input validation**
No aplica en este bloque — este bloque no recibe input externo (es el modelo y la persistencia base).

**Error handling**
- Violación de constraint de EF Core (p. ej. `Usuario` duplicado) → `ValidationException` en el
  repositorio, capturada por el middleware del Block 3 (no implementado todavía, documentado para
  consistencia).

**Required tests**
- [ ] `Usuario` con datos válidos se persiste y se recupera por `UsuarioId` — soporta AC-01
- [ ] Migración aplicada crea el perfil "administrador" y es recuperable — soporta AC-12
- [ ] Intentar persistir dos `Usuario` con el mismo `Usuario` (nombre) viola el índice único —
      soporta la integridad que AC-01/AC-03 asumen implícitamente

**Completion criterion**
`dotnet build` de la solución compila sin errores, `dotnet ef database update` aplica la migración
inicial y deja el perfil "administrador" en la tabla `Perfiles`, y los 3 tests del bloque pasan.

## Block 2 — Seguridad de credenciales

**Files**
- `src/Modulo5.Domain/Security/IPasswordHasher.cs` (new)
- `src/Modulo5.Domain/Security/Pbkdf2PasswordHasher.cs` (new)
- `src/Modulo5.Domain/Security/PasswordPolicy.cs` (new) — valida longitud/formato

**Logic**
`Pbkdf2PasswordHasher.Hash(password)` genera un salt aleatorio de 16 bytes
(`RandomNumberGenerator.Fill`) y deriva el hash con `Rfc2898DeriveBytes.Pbkdf2`, algoritmo
HMAC-SHA256, **210.000 iteraciones** (mitigación de riesgo #6 del threat model — por debajo de la
recomendación OWASP 2023 el hash es crackeable en tiempos prácticos). `Verify(password, hash, salt)`
deriva el hash del password candidato con el mismo salt y compara con
`CryptographicOperations.FixedTimeEquals` (tiempo constante — mitigación de riesgo #8, evita filtrar
por timing si la contraseña es parcialmente correcta). `PasswordPolicy.IsValid(password)` exige
longitud ≥ 8 y que sea alfanumérico (letras y dígitos únicamente).

**Input validation**
- `password`: string, longitud mínima 8, debe contener solo caracteres alfanuméricos (regex
  `^[a-zA-Z0-9]+$`). Si no cumple → `ValidationException("La contraseña debe tener al menos 8
  caracteres alfanuméricos.")` (mensaje exacto de FR-06/AC-06).

**Error handling**
- Password que no cumple la política → `ValidationException` con el mensaje exacto de AC-06,
  propagada al llamador (Block 4 la captura vía el middleware del Block 3).

**Required tests**
- [ ] El `Hash` persistido para un usuario nunca es igual al password en texto plano, y el `Salt` no
      está vacío — soporta AC-04 (verifica que la contraseña no se almacena recuperable)
- [ ] Dos usuarios con la misma contraseña producen salts distintos y hashes distintos — soporta
      AC-05
- [ ] `Verify` devuelve `true` para la contraseña correcta y `false` para una incorrecta — soporta
      AC-04
- [ ] Contraseña de 7 caracteres alfanuméricos es rechazada con el mensaje exacto — soporta AC-06
      (sad path)
- [ ] Contraseña de 8 caracteres alfanuméricos es aceptada — soporta AC-07

**Completion criterion**
Los 4 tests de `Modulo5.Domain.Tests` para este bloque pasan; `Pbkdf2PasswordHasher` no depende de
ningún tipo de `Modulo5.Data` ni `Modulo5.Api`.

## Block 3 — Api base: autenticación JWT + manejo de errores

**Files**
- `src/Modulo5.Api/Modulo5.Api.csproj` (new)
- `src/Modulo5.Api/Program.cs` (new) — configuración de JwtBearer, rate limiting, HTTPS, middleware
  de excepciones
- `src/Modulo5.Api/Middleware/ExceptionHandlingMiddleware.cs` (new)
- `src/Modulo5.Api/Controllers/AuthController.cs` (new)
- `src/Modulo5.Api/Dtos/LoginRequest.cs` (new)
- `src/Modulo5.Api/Dtos/LoginResponse.cs` (new)
- `src/Modulo5.Domain/Security/IAuthenticationService.cs` (new)
- `src/Modulo5.Domain/Security/AuthenticationService.cs` (new) — valida credenciales (usa
  `IUsuarioRepository` + `IPasswordHasher`); NO conoce JWT, solo devuelve el `Usuario` autenticado o
  falla
- `src/Modulo5.Api/Security/JwtTokenGenerator.cs` (new) — toma el `Usuario` que devolvió
  `AuthenticationService` y emite el JWT firmado

**Logic**
Separación de capas (fix del WARN del arch-audit): `AuthenticationService` (Domain) valida
usuario/contraseña — es lógica de negocio pura, sin dependencia de `Microsoft.AspNetCore.*`.
`JwtTokenGenerator` (Api) es infraestructura: toma el resultado de `AuthenticationService` y firma el
token con la clave leída de configuración (`Jwt__SigningKey`, variable de entorno — mitigación de
riesgo #1). `AuthController.Login` orquesta ambos y no contiene lógica de validación propia.
`ExceptionHandlingMiddleware` captura `ValidationException` → 400, `NotFoundException` → 404,
`UnauthorizedDomainException` → 401, cualquier otra excepción → 500 con mensaje genérico (nunca el
stack trace). El pipeline de `Program.cs` aplica `UseHttpsRedirection`/`UseHsts` fuera de
Development, y `AddRateLimiter` con una política de 5 requests/minuto por IP sobre
`POST /api/auth/login` (mitigación de riesgo #7).

**API contract**
- Method + path: `POST /api/auth/login`
- Request: `{ "usuario": string, "password": string }`
- Response 200: `{ "token": string, "expiraEn": "2026-08-17T13:30:00Z" }`
- Response 400: `{ "mensaje": "Usuario o contraseña incorrectos" }` — mismo mensaje para usuario
  inexistente y para password incorrecta (mitigación de riesgo #8, evita enumeración)
- Response 429: `{ "mensaje": "Demasiados intentos, intente nuevamente en unos minutos." }` — al
  superar el rate limit
- Auth: ninguna (es el endpoint de login, excluido de la exigencia de JWT por FR-09)

**Input validation**
- `usuario`: string, requerido, máx. 50 caracteres.
- `password`: string, requerido, sin límite superior de longitud (se hashea, no se persiste tal
  cual).

**Error handling**
- Usuario inexistente o password incorrecta → `UnauthorizedDomainException` → 400 con el mensaje
  uniforme (ver arriba; el PRD especifica este caso como 400 lógico de aplicación, no 401 HTTP,
  porque no hay sesión previa que rechazar).
- Sin JWT o JWT inválido/expirado en un endpoint protegido → el middleware `JwtBearer` responde 401
  automáticamente — soporta AC-11.
- Más de 5 intentos de login por IP en un minuto → 429.

**Required tests**
- [ ] Login con credenciales válidas devuelve 200 y un JWT bien formado — soporta AC-10
- [ ] Login con usuario inexistente devuelve 400 con el mensaje uniforme — soporta AC-09 (sad path)
- [ ] Login con password incorrecta devuelve 400 con el MISMO mensaje que el caso anterior — soporta
      AC-09 (sad path, no-enumeración)
- [ ] Request a un endpoint protegido sin header `Authorization` devuelve 401 — soporta AC-11 (sad
      path)
- [ ] Request a un endpoint protegido con JWT válido pasa la autenticación — soporta AC-11
- [ ] 6 intentos de login en menos de un minuto desde la misma IP: el 6º devuelve 429

**Completion criterion**
Los 6 tests de `Modulo5.Api.Tests` para este bloque pasan; `dotnet run --project src/Modulo5.Api`
levanta el servicio y `POST /api/auth/login` responde según el contrato.

## Block 4 — ABM de Usuarios (API)

**Files**
- `src/Modulo5.Api/Controllers/UsuariosController.cs` (new)
- `src/Modulo5.Api/Dtos/UsuarioRequest.cs` (new)
- `src/Modulo5.Api/Dtos/UsuarioResponse.cs` (new) — excluye `Hash` y `Salt`
- `src/Modulo5.Api/Authorization/AdminOnlyRequirement.cs` (new)
- `src/Modulo5.Api/Authorization/AdminOnlyHandler.cs` (new) — valida `PerfilId` del claim del JWT
  contra el perfil "administrador", no solo la presencia del token
- `src/Modulo5.Api/Program.cs` (modified) — registra la política `AdminOnly`

**Logic**
`UsuariosController` expone alta/baja/modificación, todas decoradas con
`[Authorize(Policy = "AdminOnly")]`. `AdminOnlyHandler` es la autorización real (mitigación de riesgo
#4 del threat model): lee el claim `PerfilId` del JWT y lo compara contra el `PerfilId` del perfil
"administrador" en la base — un JWT válido pero de un usuario no-administrador es rechazado con 403.
Cada operación exitosa loguea `UsuarioId` del actor (tomado del JWT) + timestamp (mitigación de
riesgo de Repudiation del threat model). `UsuarioResponse` es un DTO explícito que nunca incluye
`Hash` ni `Salt` (mitigación de riesgo #5).

**API contract**
- `POST /api/usuarios` — Request: `{ "usuario": string, "nombreCompleto": string, "password": string,
  "perfilId": int }` — Response 201: `UsuarioResponse { usuarioId, usuario, nombreCompleto, perfilId
  }` — Errores: 400 (password inválida, usuario duplicado), 403 (no-admin) — Auth: JWT + `AdminOnly`
- `PUT /api/usuarios/{id}` — Request: `{ "nombreCompleto": string, "password": string?, "perfilId":
  int }` — Response 200: `UsuarioResponse` — Errores: 400, 404, 403 — Auth: JWT + `AdminOnly`
- `DELETE /api/usuarios/{id}` — Response 204 — Errores: 404, 403 — Auth: JWT + `AdminOnly`

**Input validation**
- `usuario`: string, requerido, máx. 50 caracteres, único (F-SPEC-09 — reutiliza la constraint del
  Block 1).
- `nombreCompleto`: string, requerido, máx. 150 caracteres.
- `password`: sujeta a `PasswordPolicy` del Block 2.
- `perfilId`: int, requerido, debe existir en `Perfiles`.

**Error handling**
- `usuario` duplicado → `ValidationException` → 400 (mensaje del motor de constraint, ver Block 1).
- `password` inválida → `ValidationException` con el mensaje exacto de AC-06 → 400.
- `id` inexistente en `PUT`/`DELETE` → `NotFoundException` → 404.
- JWT válido pero `PerfilId` no-administrador → 403 (no 401: la autenticación es válida, lo que
  falla es la autorización).

**Required tests**
- [ ] Administrador da de alta un usuario válido → 201 y el usuario es recuperable — soporta AC-01
- [ ] Administrador elimina un usuario existente → 204 y ya no es recuperable — soporta AC-02
- [ ] Administrador modifica un usuario existente → 200 con los cambios persistidos — soporta AC-03
- [ ] Usuario con JWT válido pero perfil no-administrador intenta dar de alta un usuario → 403 —
      soporta AC-08 (sad path)
- [ ] Alta de usuario con `Usuario` (nombre) ya existente devuelve 400 — soporta la unicidad que
      AC-01 asume (sad path)
- [ ] Alta o modificación con una `password` que no cumple `PasswordPolicy` devuelve 400 con el
      mensaje exacto de AC-06, en el contexto del ABM — soporta AC-06 (sad path)
- [ ] `PUT`/`DELETE` sobre un `UsuarioId` inexistente devuelve 404 — soporta la integridad de
      AC-02/AC-03 (sad path)
- [ ] La respuesta de `POST`/`PUT` no incluye los campos `Hash` ni `Salt` en ningún caso — soporta el
      riesgo #5 del threat model
- [ ] El perfil "administrador" sembrado en el Block 1 es recuperable por `GET` interno usado en el
      chequeo de autorización — soporta AC-12

**Completion criterion**
Los 9 tests de `Modulo5.Api.Tests` para este bloque pasan; ningún test de respuesta serializada
contiene la cadena `"hash"` ni `"salt"` (verificación explícita anti-regresión del DTO).

## Block 5 — Pantalla Web (MVC)

**Files**
- `src/Modulo5.Web/Modulo5.Web.csproj` (new)
- `src/Modulo5.Web/Program.cs` (new) — HTTPS enforcement, `UseExceptionHandler("/Error")` fuera de
  Development
- `src/Modulo5.Web/Controllers/AccountController.cs` (new) — acción `Login`
- `src/Modulo5.Web/Controllers/UsuariosController.cs` (new) — ABM, llama a `Modulo5.Api` vía
  `HttpClient`
- `src/Modulo5.Web/Views/Account/Login.cshtml` (new)
- `src/Modulo5.Web/Views/Usuarios/Index.cshtml`, `Create.cshtml`, `Edit.cshtml` (new)
- `src/Modulo5.Web/Services/ApiClient.cs` (new) — encapsula las llamadas HTTP a `Modulo5.Api`,
  adjunta el JWT desde la cookie en el header `Authorization`

**Logic**
`AccountController.Login` llama a `POST /api/auth/login` de `Modulo5.Api` vía `ApiClient`; si es
exitoso, guarda el JWT en una cookie `HttpOnly`+`Secure`+`SameSite=Strict` (mitigación de riesgo #2
del threat model — nunca en `localStorage` ni expuesta a JS). `UsuariosController` lee esa cookie en
cada request y la reenvía como `Authorization: Bearer {token}` a `Modulo5.Api`; si la Api responde
401/403, `Modulo5.Web` redirige a `Login` o muestra "Acceso denegado" respectivamente — `Web` nunca
decide la autorización por sí mismo, solo refleja lo que la Api resuelve. Todos los formularios de
`Usuarios` (`Create`, `Edit`, eliminar) incluyen `@Html.AntiForgeryToken()` y sus acciones POST
`[ValidateAntiForgeryToken]` (mitigación de riesgo #3).

**Input validation**
Los formularios usan `[Required]`/`[StringLength]` de Data Annotations replicando las mismas reglas
del Block 4 (defensa en profundidad client+server, la validación real sigue viviendo en la Api).

**Error handling**
- Login fallido → se re-muestra `Login.cshtml` con el mensaje uniforme recibido de la Api.
- 403 de la Api en cualquier acción del ABM → vista "Acceso denegado" (no un stack trace).
- Cualquier excepción no controlada → `UseExceptionHandler("/Error")`, página genérica.

**Required tests**
*(Nota: `AGENTS.md` no declara un proyecto `Modulo5.Web.Tests` — este bloque no tiene tests
automatizados en xUnit. Los siguientes se ejecutan manualmente en la fase VERIFY, vía
`daw-verify-module`.)*
- [ ] Login exitoso navega al ABM de Usuarios
- [ ] Login fallido re-muestra la vista con el mensaje uniforme "Usuario o contraseña incorrectos"
- [ ] Un formulario del ABM enviado sin el antiforgery token es rechazado
- [ ] El ABM de Usuarios es inaccesible (vista "Acceso denegado") para un usuario autenticado
      no-administrador
- [ ] Provocar una excepción no controlada (p. ej. la Api no disponible) muestra la página de error
      genérica, sin stack trace

**Completion criterion**
`dotnet run --project src/Modulo5.Web` levanta el sitio, el login contra un usuario sembrado
funciona end-to-end contra `Modulo5.Api`, y el ABM de Usuarios queda operativo para el administrador
y bloqueado para cualquier otro perfil (verificado manualmente, ver nota de tests arriba).

## Final verification

- Los 5 bloques compilan juntos (`dotnet build` de `Modulo5.sln`) sin warnings de nulabilidad.
- Los 23 tests automatizados de `Modulo5.Domain.Tests` + `Modulo5.Api.Tests` pasan (`dotnet test`),
  más los 5 tests manuales del Block 5 verificados en VERIFY.
- Ninguna respuesta serializada de la Api contiene `Hash` ni `Salt`.
- `git grep` sobre `appsettings.json` no encuentra ninguna connection string ni clave de firma JWT en
  texto plano (verificación anti-regresión del riesgo #1 del threat model).
- Las 12 ACs del PRD (`docs/daw/prd/prd-FEAT-001a.md`) quedan cubiertas: AC-01 a AC-03 (Block 1/4/5),
  AC-04 a AC-07 (Block 2), AC-08 a AC-11 (Block 3/4), AC-12 (Block 1).

## Rollback plan

- **Migración de base de datos (Block 1):** cada migración de EF Core tiene su `Down()` generado
  automáticamente; revertir con `dotnet ef database update <migración anterior>` elimina las tablas
  `Usuarios`/`Perfiles` y el seed del administrador. Indicador: la migración inicial falla en
  producción o corrompe datos existentes (no aplica todavía — es la primera migración del proyecto).
- **Resto de los bloques:** no hay datos persistentes propios fuera de la migración del Block 1;
  revertir es `git revert` del/los commit(s) del bloque correspondiente y volver a desplegar.
