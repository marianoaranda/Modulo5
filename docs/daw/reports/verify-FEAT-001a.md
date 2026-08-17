# /daw-verify-module — FEAT-001a (Autenticación: Usuarios, Credenciales y Login JWT)

**Resultado: PASSED** — 0 FAILs, 5 WARNs (no bloqueantes)
Verificado por: agente `daw-module-verifier` (independiente, no escribió el código)

## Trazabilidad PRD → Código → Tests

| AC | Descripción | Implementación | Test |
|----|---|---|---|
| AC-01 | Alta de usuario | `UsuariosController.Create` (`src/Modulo5.Api/Controllers/UsuariosController.cs:48`) | `Modulo5DbContextTests.Usuario_con_datos_validos_se_persiste...` + `UsuariosControllerTests.Administrador_da_de_alta_un_usuario_valido...` (201, DB real, PerfilId persistido) |
| AC-02 | Baja de usuario | `UsuariosController.Delete` (`:117`) | `Administrador_elimina_un_usuario_existente...` (204 + ausencia real en DB) |
| AC-03 | Modificación de usuario | `UsuariosController.Update` (`:86`) | `Administrador_modifica_un_usuario_existente...` (body + persistencia real) |
| AC-04 | Hash de password | `Pbkdf2PasswordHasher.Hash/Verify` | `Hash_nunca_es_igual_al_password...` + `Verify_devuelve_true...` |
| AC-05 | Salt aleatorio por usuario | `Pbkdf2PasswordHasher.Hash` | `Dos_usuarios_con_la_misma_password...` (compara salts y hashes distintos) |
| AC-06 | Política de password (rechazo) | `PasswordPolicy.Validate` (`:32`) | `Password_de_7_caracteres...` + `Alta_con_password_que_no_cumple...` (mensaje exacto) |
| AC-07 | Política de password (aceptación) | `PasswordPolicy.IsValid` (`:21`) | `Password_de_8_caracteres_alfanumericos_es_aceptada` |
| AC-08 | Autorización AdminOnly | `AdminOnlyHandler` + `[Authorize(Policy="AdminOnly")]` | `Usuario_con_JWT_valido_pero_no_administrador...` (login real, 403) |
| AC-09 | No enumeración de usuarios en login | `AuthenticationService.AuthenticateAsync` (`:43`) | `Login_con_usuario_inexistente...` + `Login_con_password_incorrecta...` (mismo mensaje) + `AuthenticationServiceTests` (fix timing-channel, ronda 2) |
| AC-10 | Login devuelve JWT válido | `AuthController.Login` (`:32`) + `JwtTokenGenerator` | `Login_con_credenciales_validas_devuelve_200_y_un_JWT_bien_formado` (estructura JWT, expiración futura) |
| AC-11 | Endpoints protegidos por JWT | Middleware `JwtBearer` (`Program.cs`) | `...sin_Authorization_devuelve_401` + `...con_JWT_valido_pasa_la_autenticacion` |
| AC-12 | Perfil administrador sembrado | `PerfilConfiguration.Configure/HasData` (`:28`) | `Migracion_aplicada_crea_el_perfil_administrador...` + `El_perfil_administrador_sembrado_en_Block1...` |

Las 12 ACs tienen test PASSING que verifica comportamiento real (body/mensaje/estado en DB), ninguna superficial (status-code-only).

## Tareas del spec (F-VER-02 / F-VER-06)

- ✅ Block 1 (Domain/Data base) — 3/3 tests requeridos, verdes
- ✅ Block 2 (seguridad de credenciales) — 5/5 tests requeridos, verdes
- ✅ Block 3 (Api base: JWT + errores) — 6/6 tests requeridos, verdes (incluye rate limiting 429)
- ✅ Block 4 (ABM Usuarios) — 9/9 tests requeridos, verdes (incluye verificación anti-regresión de que la respuesta no expone hash/salt)
- ⚠️ Block 5 (pantalla Web MVC) — código completo y coherente con el spec (Login, ABM Usuarios, AccesoDenegado, Error genérico, AntiForgeryToken en los 3 forms mutables). El spec documenta explícitamente que este bloque se verifica con **5 tests manuales en VERIFY** (no hay `Modulo5.Web.Tests`). No hay artefacto que confirme que ya se ejecutaron:
  1. Login exitoso → navega
  2. Login fallido → mensaje uniforme
  3. Form sin antiforgery token → rechazado
  4. ABM inaccesible para usuario no-admin
  5. Excepción no controlada → página genérica sin stack trace

## Evidencia TDD

⚠️ No quedó persistido un artefacto con el conteo "N tests fallando antes de implementar" por bloque (los reportes del `daw-implementer` no se guardaron en disco). Evidencia circunstancial fuerte en el propio código (comentarios en `UsuariosControllerTests.cs:25-29` documentando el fallo pre-implementación real; tests de regresión de la ronda 2 en `AuthenticationServiceTests.cs`). No se marca FAIL — no hay contradicción, solo falta el artefacto explícito.

## Cobertura (F-VER-03)

`dotnet test --collect:"XPlat Code Coverage"`: **28/28 tests pasan** (13 Domain + 15 Api, coincide con lo declarado en `sast-FEAT-001a.md`).

- ✅ Cobertura de líneas sobre código de producción nuevo (excluyendo migraciones EF autogeneradas y el `DbContextFactory` de diseño-tiempo): **377/423 = 89.1%** — por encima del mínimo 80%. Lógica core en `Modulo5.Domain` (`AuthenticationService`, `Pbkdf2PasswordHasher`, `PasswordPolicy`) al 100%.
- ⚠️ Si se incluyen migraciones/`DbContextFactory` (0% cobertura, código generado, nunca ejercido en runtime), la cifra agregada baja a ~59-85% según el proyecto. No es FAIL (no es lógica de negocio) pero se deja explícito.
- ⚠️ `UsuariosController.EnsurePerfilExistsAsync` (`:138`) al 66.7% — no hay test de "alta/modificación con `perfilId` inexistente". Gap real, menor, no bloqueante (el 89% agregado ya supera el mínimo y el spec no lo exigía explícitamente bajo "Error handling").

## Sad paths (F-VER-04)

✅ Todo endpoint que acepta input tiene al menos un test con input inválido: login (usuario inexistente, password incorrecta, rate limit 429), alta de usuario (duplicado, password inválida), PUT/DELETE con id inexistente (404), endpoint protegido sin JWT (401).

## Calidad

- ✅ F-VER-05: `dotnet build Modulo5.sln` — 0 Warnings, 0 Errors
- ✅ Lint: `dotnet format --verify-no-changes` — 0 archivos con cambios
- ⚠️ W-VER-01: dos overloads muertos — `NotFoundException(string, Exception)` y `UnauthorizedDomainException(string, Exception)` nunca se invocan en producción. Código muerto literal, no rompe nada.
- ⚠️ La rama `catch (Exception ex)` genérica (500) de `ExceptionHandlingMiddleware` (58.6% branch coverage) no está ejercitada por ningún test — es una mitigación de Information Disclosure citada en el threat model pero no verificada automáticamente. No bloqueante (no listada como test requerido del Block 3).
- ✅ Sin código comentado, TODO/FIXME/HACK, ni duplicación evidente
- ✅ Ningún `appsettings.json` de `Modulo5.Api` commiteado; `Modulo5.Web` solo tiene `ApiClient:BaseUrl` (no sensible), consistente con el SAST

## Veredicto

**PASSED** — 12 ACs ✅ · 4 bloques automatizados ✅ · 3 gates de calidad ✅ · 5 WARNs no bloqueantes.

**Recomendación antes de RELEASE:** dejar evidencia escrita de los 5 tests manuales del Block 5 (Web) exigidos por el propio spec, ya que hoy no hay artefacto que confirme que se corrieron.
