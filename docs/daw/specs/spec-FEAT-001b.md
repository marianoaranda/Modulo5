# Spec FEAT-001b: ABM de Artículos

| Field | Value |
|-------|-------|
| Ticket | FEAT-001b |
| PRD | docs/daw/prd/prd-FEAT-001b.md |
| Tier | FEATURE |
| Date | 2026-08-18 |
| Spec loops | 0 |

## Summary

Se agrega el ABM de Artículos sobre la solución existente (FEAT-001a): una entidad `Articulo` nueva
en Domain/Data, un controller REST protegido solo por JWT (sin restricción de perfil, a diferencia de
Usuarios) y una pantalla Web que replica el patrón ya usado por el ABM de Usuarios. El cálculo del
Precio de Venta (FR-04) y las validaciones de negocio sin dependencia de base de datos (FR-06, FR-07)
viven en Domain como clases estáticas testeables, siguiendo el mismo patrón que `PasswordPolicy` de
FEAT-001a — no inline en el controller.

## Coverage: PRD → blocks

| Requirement | Covered by |
|---|---|
| FR-01 | Block 1 (modelo), Block 2 (endpoint), Block 3 (UI) |
| FR-02 | Block 1 (modelo), Block 2 (endpoint), Block 3 (UI) |
| FR-03 | Block 1 (modelo), Block 2 (endpoint), Block 3 (UI) |
| FR-04 | Block 1 (`PrecioVentaCalculator`), Block 2 (lo invoca) |
| FR-05 | Block 1 (constraint + traducción en el repository), Block 2 (surge como 400) |
| FR-06 | Block 1 (`ArticuloValidationPolicy`), Block 2 (lo invoca) |
| FR-07 | Block 1 (`ArticuloValidationPolicy`), Block 2 (lo invoca) |
| NFR-01 | Strategy: ya cubierto por `Modulo5.Web` (ASP.NET MVC .NET 8), creado en FEAT-001a |
| NFR-02 | Strategy: ya cubierto por `Modulo5.Api` (Web API REST .NET 8 + JWT), creado en FEAT-001a; Block 2 solo agrega `[Authorize]` |
| NFR-03 | Strategy: ya cubierto por `Modulo5.Data` (EF Core 8 sobre SQL Server 2017), creado en FEAT-001a |
| NFR-04 | Strategy: la arquitectura por capas y el pool de conexiones por defecto de EF Core (heredados de FEAT-001a) soportan 1-5 usuarios concurrentes sin configuración adicional |

## Dependencies between blocks

- Block 2 depende de Block 1 (usa `Articulo`, `IArticuloRepository`, `PrecioVentaCalculator`,
  `ArticuloValidationPolicy`).
- Block 3 depende de Block 2 (consume `POST/PUT/DELETE /api/articulos`).

Orden de ejecución: 1 → 2 → 3.

## Block 1 — Domain y Data: entidad Articulo

**Files**
- `src/Modulo5.Domain/Entities/Articulo.cs` (new)
- `src/Modulo5.Domain/Repositories/IArticuloRepository.cs` (new)
- `src/Modulo5.Domain/Articulos/PrecioVentaCalculator.cs` (new)
- `src/Modulo5.Domain/Articulos/ArticuloValidationPolicy.cs` (new)
- `src/Modulo5.Data/Configurations/ArticuloConfiguration.cs` (new)
- `src/Modulo5.Data/Repositories/ArticuloRepository.cs` (new)
- `src/Modulo5.Data/Modulo5DbContext.cs` (modified) — agrega `DbSet<Articulo>` y
  `ApplyConfiguration(new ArticuloConfiguration())`
- `src/Modulo5.Data/Migrations/*_AddArticulos.cs` (new, generada con `dotnet ef migrations add`)
- `tests/Modulo5.Domain.Tests/Articulos/PrecioVentaCalculatorTests.cs` (new)
- `tests/Modulo5.Domain.Tests/Articulos/ArticuloValidationPolicyTests.cs` (new)

**Logic**
`Articulo` no tiene un identificador autonumérico separado: el PRD (AC-01 "persistirlo y permitir su
recuperación por Código", AC-02 "ya no pueda recuperarse por su Código") nunca menciona un ID técnico,
a diferencia de `Usuario`/`Perfil` (que sí lo tienen porque su PRD original así lo definía). Por eso
`Codigo` es la PK natural de la tabla — decisión de modelado específica de esta entidad, no un cambio
de convención general.

`PrecioVentaCalculator.Calcular(precioCosto, margen)` implementa FR-04 de forma pura (sin estado, sin
dependencias de Data/Api), igual patrón que `Domain/Security/PasswordPolicy.cs` de FEAT-001a.
`ArticuloValidationPolicy.Validate(precioCosto, margen, stockMinimo, puntoPedido, stockIdeal)` aplica
FR-06 y FR-07 y lanza `ValidationException` con los mensajes exactos del PRD — es una regla de negocio
sin dependencia de base de datos, por lo que vive en Domain (no en el controller de Api), igual
patrón que `PasswordPolicy.Validate`.

`ArticuloRepository` traduce la violación de PK (Código duplicado) a `ValidationException`, igual
patrón que `UsuarioRepository.SaveChangesTranslatingConstraintViolationsAsync`.

**Data model**

`Articulo`
| Field | Type | Constraints |
|---|---|---|
| Codigo | nvarchar(30) | PK |
| Descripcion | nvarchar(200) | not null |
| PrecioCosto | decimal(18,2) | not null |
| Margen | decimal(5,2) | not null |
| PrecioVenta | decimal(18,2) | not null — calculado por `PrecioVentaCalculator`, nunca aceptado tal cual del cliente |
| StockMinimo | int | not null |
| PuntoPedido | int | not null |
| StockIdeal | int | not null |

**Input validation**
No aplica directamente en este bloque (no recibe input HTTP); `ArticuloValidationPolicy` sí valida
los valores que Block 2 le pasa: `precioCosto`/`margen`/`stockMinimo`/`puntoPedido`/`stockIdeal` no
negativos, y `stockMinimo ≤ puntoPedido ≤ stockIdeal`.

**Error handling**
- `ArticuloValidationPolicy.Validate` con algún valor negativo → `ValidationException("Los valores
  de Precio de Costo, Margen, Stock Mínimo, Punto de Pedido y Stock Ideal no pueden ser negativos.")`
  (FR-06).
- `ArticuloValidationPolicy.Validate` con el orden roto → `ValidationException("El Stock Mínimo debe
  ser menor o igual al Punto de Pedido, y este menor o igual al Stock Ideal.")` (FR-07).
- Violación de PK (Código duplicado) en el repository → `ValidationException("Ya existe un artículo
  con el Código ingresado.")` (FR-05), capturada por el middleware existente (Block 3 de FEAT-001a,
  sin cambios).

**Required tests**
- [ ] `PrecioVentaCalculator.Calcular(100, 20)` devuelve `120` — soporta AC-04
- [ ] `PrecioVentaCalculator.Calcular` con `Margen = 0` devuelve el mismo `PrecioCosto` — soporta
      AC-04 (edge case)
- [ ] `ArticuloValidationPolicy.Validate` rechaza `PrecioCosto`, `Margen`, `StockMinimo`,
      `PuntoPedido` o `StockIdeal` negativos con el mensaje exacto de AC-06 (sad path)
- [ ] `ArticuloValidationPolicy.Validate` rechaza `StockMinimo > PuntoPedido` con el mensaje exacto
      de AC-07 (sad path)
- [ ] `ArticuloValidationPolicy.Validate` rechaza `PuntoPedido > StockIdeal` con el mensaje exacto de
      AC-07 (sad path)
- [ ] `ArticuloValidationPolicy.Validate` acepta `StockMinimo == PuntoPedido == StockIdeal` (límites
      inclusive) — soporta AC-07 (edge case)
- [ ] Un `Articulo` con datos válidos se persiste y se recupera por `Codigo` — soporta AC-01
- [ ] Persistir dos `Articulo` con el mismo `Codigo` viola la PK — soporta la integridad que AC-01/
      AC-05 asumen

**Completion criterion**
`dotnet build` de la solución compila sin errores, `dotnet ef database update` aplica la migración
`AddArticulos`, y los 8 tests de `Modulo5.Domain.Tests` de este bloque pasan.

## Block 2 — Api: ABM de Artículos

**Files**
- `src/Modulo5.Api/Controllers/ArticulosController.cs` (new)
- `src/Modulo5.Api/Dtos/ArticuloRequest.cs` (new)
- `src/Modulo5.Api/Dtos/ArticuloResponse.cs` (new)
- `src/Modulo5.Api/Program.cs` (modified) — registra
  `AddScoped<IArticuloRepository, ArticuloRepository>()`, mismo punto donde ya están registrados
  `IUsuarioRepository`/`IPerfilRepository` (líneas 42-43)
- `tests/Modulo5.Api.Tests/ArticulosControllerTests.cs` (new)

**Logic**
`ArticulosController` expone alta/baja/modificación, decoradas solo con `[Authorize]` (JWT) — sin la
política `AdminOnly` de Usuarios: el PRD de Artículos (a diferencia de Usuarios/RF-10) no restringe
esta pantalla a ningún perfil. Antes de persistir (alta y modificación), el controller invoca
`ArticuloValidationPolicy.Validate` (Block 1) y siempre recalcula `PrecioVenta` con
`PrecioVentaCalculator.Calcular`, ignorando cualquier `PrecioVenta` que venga en el request. El
Código duplicado lo traduce el repository (Block 1), no el controller. `PUT`/`DELETE` sobre un
`Codigo` inexistente lanzan `NotFoundException`. Cada operación exitosa (alta/modificación/baja)
loguea el `UsuarioId` del actor (tomado del claim del JWT) + `Codigo` del artículo + operación +
timestamp — mitigación de Repudiation del threat model (`docs/daw/security/threat-FEAT-001b.md`,
riesgo #3), mismo patrón que `UsuariosController.LogOperacionExitosa` de FEAT-001a.

**API contract**
- `POST /api/articulos` — Request: `{ "codigo": string, "descripcion": string, "precioCosto":
  decimal, "margen": decimal, "stockMinimo": int, "puntoPedido": int, "stockIdeal": int }` —
  Response 201: `ArticuloResponse { codigo, descripcion, precioCosto, margen, precioVenta,
  stockMinimo, puntoPedido, stockIdeal }` — Errores: 400 (código duplicado, valores negativos,
  umbrales fuera de orden), 401 (sin JWT) — Auth: JWT
- `PUT /api/articulos/{codigo}` — Request: `{ "descripcion": string, "precioCosto": decimal,
  "margen": decimal, "stockMinimo": int, "puntoPedido": int, "stockIdeal": int }` (`Codigo` no es
  editable, viene de la ruta) — Response 200: `ArticuloResponse` — Errores: 400, 404, 401 — Auth: JWT
- `DELETE /api/articulos/{codigo}` — Response 204 — Errores: 404, 401 — Auth: JWT

**Input validation**
- `codigo` (solo en POST, en PUT viene de la ruta): string, requerido, máx. 30 caracteres.
- `descripcion`: string, requerido, máx. 200 caracteres.
- `precioCosto`, `margen`, `stockMinimo`, `puntoPedido`, `stockIdeal`: sujetos a
  `ArticuloValidationPolicy` del Block 1.

**Error handling**
- `codigo` duplicado (alta) → `ValidationException` (repository, Block 1) → 400.
- Valores negativos → `ValidationException` (`ArticuloValidationPolicy`) → 400, mensaje exacto FR-06.
- Umbrales fuera de orden → `ValidationException` (`ArticuloValidationPolicy`) → 400, mensaje exacto
  FR-07.
- `codigo` inexistente en `PUT`/`DELETE` → `NotFoundException` → 404.
- Sin JWT válido → 401 (middleware `JwtBearer`, nativo, sin cambios).

**Required tests**
- [ ] Alta de artículo con datos válidos → 201 y recuperable por `Codigo` — soporta AC-01
- [ ] Baja de artículo existente → 204 y ya no recuperable — soporta AC-02
- [ ] Modificación de artículo existente → 200 con los cambios persistidos — soporta AC-03
- [ ] Alta calcula `PrecioVenta = PrecioCosto × (1 + Margen/100)`, ignorando cualquier `PrecioVenta`
      enviado por el cliente — soporta AC-04
- [ ] Alta con `Codigo` duplicado → 400 con el mensaje exacto de AC-05 (sad path)
- [ ] Alta o modificación con un valor negativo → 400 con el mensaje exacto de AC-06 (sad path)
- [ ] Alta o modificación que rompe `StockMinimo ≤ PuntoPedido ≤ StockIdeal` → 400 con el mensaje
      exacto de AC-07 (sad path)
- [ ] `PUT`/`DELETE` sobre un `Codigo` inexistente → 404 (sad path)
- [ ] Request sin header `Authorization` → 401 (sad path)

**Completion criterion**
Los 9 tests de `Modulo5.Api.Tests` de este bloque pasan; `dotnet run --project src/Modulo5.Api`
levanta el servicio y los 3 endpoints responden según el contrato.

## Block 3 — Pantalla Web (MVC)

**Files**
- `src/Modulo5.Web/Controllers/ArticulosController.cs` (new)
- `src/Modulo5.Web/Models/ArticuloCreateViewModel.cs` (new)
- `src/Modulo5.Web/Models/ArticuloEditViewModel.cs` (new)
- `src/Modulo5.Web/Views/Articulos/Index.cshtml` (new)
- `src/Modulo5.Web/Views/Articulos/Create.cshtml` (new)
- `src/Modulo5.Web/Views/Articulos/Edit.cshtml` (new)
- `src/Modulo5.Web/Services/ApiClient.cs` (modified) — agrega `CrearArticuloAsync`,
  `ModificarArticuloAsync`, `EliminarArticuloAsync`
- `src/Modulo5.Web/Services/ArticuloDto.cs` (new)
- `src/Modulo5.Web/Services/ArticuloRequestDto.cs` (new)

**Logic**
Mismo patrón que `Web/Controllers/UsuariosController.cs`: `ArticulosController` llama a
`Modulo5.Api` vía `ApiClient` reenviando el JWT de la cookie; si la Api responde 401 redirige a
Login, si responde 403 muestra "Acceso denegado" (reutiliza la vista existente
`Views/Usuarios/AccesoDenegado.cshtml`, movida/generalizada si hace falta, o se crea el mismo
partial para Artículos — decisión de implementación del bloque). `Index` es un panel de navegación
(alta / modificar por Código / eliminar por Código): la Api no expone ningún `GET /api/articulos`,
igual que ocurre con Usuarios, así que no hay grilla real. Los formularios de `Create`/`Edit`/`Delete`
incluyen `@Html.AntiForgeryToken()` y sus acciones POST `[ValidateAntiForgeryToken]`.

**Input validation**
Los formularios usan `[Required]`/`[StringLength]`/`[Range]` de Data Annotations replicando las
reglas del Block 2 (defensa en profundidad; la validación real vive en la Api).

**Error handling**
- 400 de la Api, cualquiera sea el motivo (duplicado, negativos, umbrales) → un único mecanismo: se
  re-muestra el formulario con el mensaje recibido en `ModelState` (mismo code path, sin distinguir
  causa).
- 404 de la Api (Código inexistente en Edit/Delete) → mensaje de error, vuelve a `Index`.
- 401 → redirect a Login. (403 no se documenta como caso manejado en este bloque: con solo
  `[Authorize]` en el Block 2, la Api nunca emite 403 para estos endpoints — a diferencia de
  Usuarios, que sí lo hace vía `AdminOnly`.)

**Required tests**
*(Nota: `AGENTS.md` no declara un proyecto `Modulo5.Web.Tests`, igual que en FEAT-001a — este bloque
no tiene tests automatizados en xUnit; se verifican manualmente en VERIFY.)*
- [ ] Alta de artículo válida navega a Index con mensaje de éxito
- [ ] Alta con Código duplicado re-muestra el formulario con el mensaje de la Api — ejercita el
      mismo code path que negativos/umbrales (un único manejo de 400)
- [ ] Modificación de un artículo existente persiste los cambios
- [ ] Baja de un artículo existente lo elimina
- [ ] Editar o eliminar un Código inexistente muestra un mensaje de error y vuelve a Index — soporta
      el 404
- [ ] Acceder a cualquier acción del ABM sin cookie JWT válida redirige a Login — soporta el 401
- [ ] Un formulario enviado sin antiforgery token es rechazado

**Completion criterion**
`dotnet run --project src/Modulo5.Web` levanta el sitio y el ABM de Artículos queda operativo
end-to-end contra `Modulo5.Api` (verificado manualmente, ver nota de tests arriba).

## Final verification

- Los 3 bloques compilan juntos (`dotnet build` de `Modulo5.sln`) sin warnings de nulabilidad.
- Los 17 tests automatizados de `Modulo5.Domain.Tests` + `Modulo5.Api.Tests` de este ticket pasan
  (`dotnet test`), más los 5 tests manuales del Block 3 verificados en VERIFY.
- Las 7 ACs del PRD (`docs/daw/prd/prd-FEAT-001b.md`) quedan cubiertas: AC-01 a AC-03 (Block 1/2/3),
  AC-04 (Block 1/2), AC-05 a AC-07 (Block 1/2).
- Ningún endpoint de `/api/articulos` es accesible sin JWT válido (401).

## Rollback plan

- **Migración de base de datos (Block 1):** la migración `AddArticulos` tiene su `Down()` generado
  automáticamente; revertir con `dotnet ef database update <migración anterior>` elimina la tabla
  `Articulos`. Indicador: la migración falla en producción o corrompe datos existentes.
- **Resto de los bloques:** no hay datos persistentes propios fuera de la migración del Block 1;
  revertir es `git revert` del/los commit(s) del bloque correspondiente y volver a desplegar.
