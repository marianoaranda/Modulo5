# Threat Model FEAT-001a: Autenticación (Usuarios, Credenciales y Login JWT)

| Field | Value |
|-------|-------|
| Ticket | FEAT-001a |
| Date | 2026-08-17 |
| Scope | Bloques 1 a 5 del plan técnico (Domain/Data base, hashing PBKDF2, Api con JWT + manejo de errores, ABM Usuarios, Web MVC) |

## Componentes y límites de confianza (F-TM-02)

```
[Navegador]  --HTTPS-->  [Modulo5.Web (MVC)]  --HTTPS-->  [Modulo5.Api]  -->  [Modulo5.Domain]  -->  [Modulo5.Data]  --TLS-->  [SQL Server]
   (no confiable)          (server-side)          (server-side)          (in-process)          (server-side)          (infra)
```

- **Boundary 1 — Navegador ↔ Modulo5.Web:** el único punto donde entra input no confiable directamente
  del usuario. Requiere HTTPS y protección CSRF.
- **Boundary 2 — Modulo5.Web ↔ Modulo5.Api:** Web actúa como cliente autenticado de la Api (posee el
  JWT tras el login). Requiere HTTPS y validación de JWT en cada request.
- **Boundary 3 — Modulo5.Api ↔ Modulo5.Domain:** in-process, mismo nivel de confianza — no es un
  boundary de red, pero Domain no debe confiar ciegamente en datos ya validados por Api (defensa en
  profundidad: Domain revalida sus propias invariantes).
- **Boundary 4 — Modulo5.Data ↔ SQL Server:** credenciales de conexión gestionadas fuera del código,
  canal cifrado (TLS).

## Análisis STRIDE por componente (F-TM-01)

### Modulo5.Web (MVC — login, ABM Usuarios)

| STRIDE | Amenaza | Mitigación |
|---|---|---|
| Spoofing | Robo de la sesión/JWT del usuario para impersonarlo | JWT almacenado en cookie `HttpOnly`, `Secure`, `SameSite=Strict` seteada por Web tras el login — nunca en `localStorage`/JS accesible |
| Tampering | CSRF sobre el ABM de Usuarios (altas/bajas/modificaciones vía formularios) | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` en todos los formularios que mutan estado |
| Repudiation | No queda registro de qué administrador hizo qué cambio sobre un usuario | El Bloque 4 debe loguear `UsuarioId` del actor + timestamp en cada operación de ABM de Usuarios |
| Information Disclosure | Página de error por defecto de ASP.NET expone stack trace en producción | `UseExceptionHandler("/Error")` + `UseHsts` fuera de Development |
| Denial of Service | No aplica de forma diferencial a Web (bajo NFR-04: 1-5 usuarios concurrentes) | — |
| Elevation of Privilege | Un usuario no-administrador accede directamente a la URL del ABM de Usuarios | La vista/controlador de Web exige el rol, pero la autorización real vive en Modulo5.Api (Web nunca es la única barrera) |

### Modulo5.Api (login, ABM Usuarios)

| STRIDE | Amenaza | Mitigación |
|---|---|---|
| Spoofing | JWT falsificado o firmado con clave débil | Firma con clave simétrica ≥256 bits desde configuración (nunca hardcodeada — ver Information Disclosure abajo); validación de firma, issuer y expiración en cada request |
| Tampering | Modificación del payload del JWT (p. ej. cambiar el PerfilId a administrador) | La validación de firma de `JwtBearer` rechaza cualquier token alterado |
| Repudiation | Intentos de login (éxito/fallo) sin registro | Loguear cada intento de login (usuario, resultado, timestamp) sin loguear la contraseña ni el hash |
| Information Disclosure | (a) Clave de firma JWT o connection string hardcodeada en el código/`appsettings.json` versionado; (b) mensaje de login que revela si el usuario existe o no | (a) Clave y connection string SOLO por variables de entorno / `dotnet user-secrets` en desarrollo — nunca en `appsettings.json` commiteado (ver Bloque 1, `.gitignore`); (b) el mensaje "Usuario o contraseña incorrectos" (AC-09) es igual para ambos casos, y la comparación de hash usa tiempo constante (`CryptographicOperations.FixedTimeEquals`) para no filtrar por timing |
| Denial of Service | Fuerza bruta / credential stuffing contra `POST /api/auth/login` | Rate limiting sobre el endpoint de login (`Microsoft.AspNetCore.RateLimiting`, nativo de .NET 8 — sin dependencia nueva), p. ej. máx. 5 intentos/minuto por IP |
| Elevation of Privilege | Un endpoint de `/api/usuarios` solo verifica que el JWT sea válido, no que el PerfilId sea administrador | Autorización explícita por política (`[Authorize(Policy = "AdminOnly")]`) evaluada en el server en CADA endpoint de Usuarios, no solo presencia de token — ver FR-07/AC-08 |

### Modulo5.Domain (AuthenticationService, PasswordHasher, entidades)

| STRIDE | Amenaza | Mitigación |
|---|---|---|
| Spoofing | N/A (in-process) | — |
| Tampering | N/A (in-process) | — |
| Repudiation | N/A (responsabilidad de Api/Data) | — |
| Information Disclosure | Hashing débil permite ataques offline si la base se filtra | PBKDF2-HMAC-SHA256 con ≥210.000 iteraciones (recomendación OWASP 2023) y salt aleatorio de 16 bytes por usuario (ya definido en AGENTS.md) |
| Denial of Service | N/A | — |
| Elevation of Privilege | Bug de lógica que no valide correctamente el PerfilId | Tests explícitos de `AuthenticationService`/autorización cubriendo AC-08 (perfil no-admin denegado) |

### Modulo5.Data (EF Core, SQL Server)

| STRIDE | Amenaza | Mitigación |
|---|---|---|
| Spoofing | N/A | — |
| Tampering | Inyección SQL | EF Core con LINQ/parametrización exclusivamente; ninguna concatenación de SQL crudo con input de usuario (si algún query requiere SQL crudo, debe documentarse por qué el ORM no alcanza) |
| Repudiation | N/A | — |
| Information Disclosure | Los campos `Hash`/`Salt` se filtran si un endpoint devuelve la entidad `Usuario` completa | Los controladores de Api SIEMPRE devuelven DTOs de respuesta que excluyen `Hash` y `Salt` — nunca la entidad de Domain/Data directamente |
| Denial of Service | N/A (fuera de alcance de NFR-04) | — |
| Elevation of Privilege | N/A | — |

## Clasificación de datos sensibles (F-TM-05)

| Dato | Clasificación | Protección |
|---|---|---|
| Hash + Salt de contraseña | Credenciales | Irreversibles por diseño (FR-04); nunca se loguean ni se devuelven en respuestas de la Api |
| Usuario, NombreCompleto | PII (baja sensibilidad) | Solo accesible vía Api autenticada; en tránsito por TLS |
| Token JWT | Credencial de sesión | Cookie `HttpOnly`/`Secure`; nunca logueado; expiración 60 min |
| Connection string / clave de firma JWT | Secreto de configuración | Variables de entorno / `user-secrets`; nunca en git (F-SAST-01) |

## Cifrado en tránsito y en reposo (F-TM-07)

- **En tránsito:** HTTPS obligatorio en los tres saltos (Navegador↔Web, Web↔Api, Api↔SQL Server vía
  `Encrypt=True` en la connection string). `UseHttpsRedirection` + `UseHsts` fuera de Development.
- **En reposo:** Hash/Salt son irreversibles por construcción (no requieren cifrado adicional para
  cumplir FR-04). Para el resto de los datos (Usuario, NombreCompleto), se recomienda habilitar
  Transparent Data Encryption (TDE) de SQL Server 2017 a nivel de base de datos como paso de
  configuración de despliegue — se documenta como prerequisito operativo del Bloque 1, no como
  código de la aplicación.

## Riesgos identificados

| # | Riesgo | STRIDE | Severidad | Mitigación |
|---|---|---|---|---|
| 1 | Clave de firma JWT / connection string hardcodeada | Information Disclosure | 🔴 CRITICAL | Variables de entorno / user-secrets; `.gitignore` actualizado (Bloque 1) |
| 2 | Robo de JWT vía XSS si se guarda en almacenamiento accesible por JS | Spoofing | 🟠 HIGH | Cookie `HttpOnly`+`Secure`+`SameSite=Strict` (Bloque 5) |
| 3 | CSRF sobre ABM de Usuarios | Tampering | 🟠 HIGH | `AntiForgeryToken` en Web (Bloque 5) |
| 4 | Autorización de `/api/usuarios` basada solo en "tiene JWT válido", sin chequear PerfilId | Elevation of Privilege | 🟠 HIGH | Política `AdminOnly` explícita, testeada (Bloque 4) |
| 5 | Hash/Salt expuestos por un endpoint que devuelve la entidad completa | Information Disclosure | 🟠 HIGH | DTOs de respuesta explícitos (Bloque 4) |
| 6 | Iteraciones de PBKDF2 insuficientes | Information Disclosure | 🟠 HIGH | ≥210.000 iteraciones, especificado explícitamente en el spec (Bloque 2) |
| 7 | Fuerza bruta / credential stuffing sobre `/api/auth/login` | Denial of Service / Spoofing | 🟡 MEDIUM | Rate limiting nativo de .NET 8 (Bloque 3) |
| 8 | Enumeración de usuarios vía mensaje de error o timing del login | Information Disclosure | 🟡 MEDIUM | Mensaje uniforme (ya en AC-09) + comparación de hash en tiempo constante (Bloque 3) |
| 9 | SQL Server sin TDE — PII en reposo sin cifrado adicional | Information Disclosure | 🟢 LOW | Recomendación de configuración de despliegue (no bloquea el código) |

Ningún riesgo quedó sin mitigación folded al spec (F-TM-03): los 9 se pliegan a los bloques 1 a 5
como se indica en la columna Mitigación. Los 6 riesgos CRITICAL/HIGH tienen mitigación concreta
folded al spec, que es el criterio de PASSED. No hay riesgos aceptados sin mitigar (F-TM-04 no
aplica — no hay ningún "accepted risk" en esta lista).

## Dependencias externas (W-TM-01)

No se agregan dependencias NuGet nuevas más allá de las ya declaradas en AGENTS.md
(`Microsoft.AspNetCore.Authentication.JwtBearer`, EF Core, `Microsoft.AspNetCore.RateLimiting` —
esta última nativa de ASP.NET Core 8, sin paquete adicional). Sin riesgo de cadena de suministro
nuevo.

## Resumen

Risks: C:1 H:5 M:2 L:1 — los 6 Critical/High tienen mitigación folded al spec (F-TM-03); ninguno
queda sin mitigar; no hay riesgos aceptados (F-TM-04 no aplica).
