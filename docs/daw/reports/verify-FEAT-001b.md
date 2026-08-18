# Verificación Final FEAT-001b — ABM de Artículos

| Field | Value |
|-------|-------|
| Ticket | FEAT-001b |
| Date | 2026-08-18 |
| PRD | docs/daw/prd/prd-FEAT-001b.md |
| Spec | docs/daw/specs/spec-FEAT-001b.md |
| Threat model | docs/daw/security/threat-FEAT-001b.md |
| SAST report | docs/daw/security/sast-FEAT-001b.md |
| Verdict | **PASSED** (0 FAILs, 3 WARNINGs no bloqueantes) |

## F-VER-01 — Cada AC del PRD tiene un test que pasa

`dotnet test Modulo5.sln` (salida real):

```
Passed!  - Failed: 0, Passed: 25, Skipped: 0, Total: 25, Duration: 1 s - Modulo5.Domain.Tests.dll (net8.0)
Passed!  - Failed: 0, Passed: 24, Skipped: 0, Total: 24, Duration: 4 s - Modulo5.Api.Tests.dll (net8.0)
```

49/49 tests en verde (25 Domain + 24 Api), coincide con el cierre de CODE.

| AC | Test(s) | Calidad |
|---|---|---|
| ✅ AC-01 (FR-01) | `Modulo5DbContextTests.Articulo_con_datos_validos_se_persiste_y_se_recupera_por_Codigo` + `ArticulosControllerTests.Alta_de_articulo_con_datos_validos_devuelve_201_y_es_recuperable` | Verifica body 201 **y** el registro persistido leyendo el DbContext directamente |
| ✅ AC-02 (FR-02) | `ArticulosControllerTests.Baja_de_articulo_existente_devuelve_204_y_ya_no_es_recuperable` | Verifica 204 **y** que `FindAsync` devuelve `null` después |
| ✅ AC-03 (FR-03) | `ArticulosControllerTests.Modificacion_de_articulo_existente_devuelve_200_con_cambios_persistidos` | Verifica body 200 **y** el cambio persistido en DB |
| ✅ AC-04 (FR-04) | `PrecioVentaCalculatorTests` (base 100/20→120, edge Margen=0) + `ArticulosControllerTests.Alta_calcula_PrecioVenta_ignorando_el_enviado_por_el_cliente` | El test de Api envía `precioVenta: 999999` y verifica `120` — cubre la mitigación de Tampering del threat model |
| ✅ AC-05 (FR-05) | `Modulo5DbContextTests.Persistir_dos_Articulo_con_el_mismo_Codigo_viola_la_PK` + `ArticulosControllerTests.Alta_con_Codigo_duplicado_devuelve_400_con_mensaje_exacto` | Verifica el mensaje exacto del PRD |
| ✅ AC-06 (FR-06) | `ArticuloValidationPolicyTests` (Theory, 5 casos) + `ArticulosControllerTests.Alta_o_modificacion_con_valor_negativo_devuelve_400_con_mensaje_exacto` | Mensaje exacto verificado en ambas capas |
| ✅ AC-07 (FR-07) | `ArticuloValidationPolicyTests` (StockMinimo>PuntoPedido, PuntoPedido>StockIdeal, límites inclusive) + `ArticulosControllerTests.Alta_o_modificacion_que_rompe_orden_de_stock_devuelve_400_con_mensaje_exacto` | Incluye el edge case de igualdad |

**F-VER-01: ✅ PASS**

## F-VER-02 — Cada tarea del spec está implementada

Los 3 bloques están implementados; verificado archivo por archivo contra "Files"/"Logic" del spec:

- **Block 1** (`da4594c`): `Articulo.cs`, `IArticuloRepository.cs`, `PrecioVentaCalculator.cs`, `ArticuloValidationPolicy.cs`, `ArticuloConfiguration.cs`, `ArticuloRepository.cs`, `Modulo5DbContext.cs` (modificado), migración `AddArticulos`.
- **Block 2** (`bd24236`): `ArticulosController.cs`, `ArticuloRequest.cs`, `ArticuloResponse.cs`, `Program.cs`. Confirmado en código:
  - **Mitigación #2 (recálculo server-side de PrecioVenta):** `Create`/`Update` llaman siempre `PrecioVentaCalculator.Calcular(...)`; `ArticuloRequest` ni siquiera declara un campo `PrecioVenta`.
  - **Mitigación #3 (logging de auditoría):** `LogOperacionExitosa` loguea actor (claim JWT) + Código + operación + timestamp en las 3 operaciones.
- **Block 3** (`6b1380f`): controller Web, ViewModels, 3 vistas Razor, `ApiClient.cs` (3 métodos nuevos), DTOs. `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` en los 3 formularios.

**F-VER-02: ✅ PASS**

## F-VER-03 — Cobertura ≥80% líneas/branches/funciones sobre código nuevo

El proyecto tiene `coverlet.collector`. Corrida: `dotnet test --collect:"XPlat Code Coverage"`, agregado manual sobre `coverage.cobertura.xml` (sin `reportgenerator` instalado, cálculo manual documentado como tal):

| Clase | Line-rate | Branch-rate |
|---|---|---|
| `Articulo`, `PrecioVentaCalculator`, `ArticuloValidationPolicy`, `ArticuloConfiguration`, `ArticuloRepository`, DTOs | 100% | 100% |
| `ArticulosController` (Api) | 84.21% | 50% |

Agregado: **Líneas 362/374 = 96.8%** ✅ · **Branches 48/60 = 80.0%** ✅ (justo en el mínimo) · **Funciones 100%** ✅.

El branch-rate bajo de `ArticulosController` viene de 2 ramas sin cubrir en `ValidateCodigo`/`ValidateDescripcion` (ver hallazgo adicional abajo).

**F-VER-03: ✅ PASS** (branch-rate en el piso exacto, no por debajo).

## F-VER-04 — Sad-path por endpoint/función con input

| Endpoint/función | Sad-path test |
|---|---|
| `POST /api/articulos` | ✅ 400 duplicado, ✅ 400 negativos, ✅ 400 orden de stock, ✅ 401 sin auth |
| `PUT /api/articulos/{codigo}` | ✅ 404 inexistente; negativos/orden comparten `ArticuloValidationPolicy.Validate` ya testeado |
| `DELETE /api/articulos/{codigo}` | ✅ 404 inexistente |
| `ArticuloValidationPolicy.Validate` | ✅ 5 sad paths de negativos + 2 de orden de stock |

**F-VER-04: ✅ PASS**

## F-VER-05 — Lint/type checker sin errores

```
$ dotnet build Modulo5.sln
Build succeeded. 0 Warning(s) 0 Error(s)

$ dotnet format Modulo5.sln --verify-no-changes
(exit code 0, sin diferencias)
```

**F-VER-05: ✅ PASS**

## F-VER-06 — Cada test listado en el spec existe y pasa

- **Block 1 (8 tests):** los 8 existen y pasan. ✅
- **Block 2 (9 tests):** los 9 existen como `[Fact]` en `ArticulosControllerTests.cs` y pasan. ✅
- **Block 3 (7 tests manuales, no automatizados):** verificados por inspección de código (sin ejecución en navegador, no disponible en este entorno) — los 7 code paths existen y hacen lo que el bullet describe. Nota sobre el bullet #6 ("sin JWT redirige a Login"): en código, solo las acciones POST llaman a la Api y pueden recibir 401 — las acciones GET no llaman a la Api, igual patrón que `UsuariosController` de FEAT-001a (decisión ya aprobada, documentada en `Modulo5.Web/Program.cs`). No es una desviación de este ticket, pero el bullet es impreciso en su alcance.

**F-VER-06: ✅ PASS** (con la salvedad metodológica de Block 3, verificación por inspección de código, no ejecución real).

## Nota — inconsistencia de prosa en el spec

La sección "Final verification" del spec dice "5 tests manuales del Block 3", pero el propio Block 3 lista 7 (se agregaron 2 en el loop de PLAN para cumplir F-SPEC-16 y el número de resumen no se actualizó). No cuenta como FAIL de F-VER-02/06 — los 7 tests existen y están trazados; es solo una cifra vieja en una oración de resumen del documento. No se corrige (el spec no se edita fuera de PLAN).

## W-VER-01 — Dead code / imports sin usar
✅ Sin hallazgos — `dotnet format --verify-no-changes` limpio, `using` de los 18 archivos nuevos revisados manualmente.

## W-VER-02 — Cobertura de lógica de negocio 80-90%
✅ `PrecioVentaCalculator`/`ArticuloValidationPolicy` al 100% línea/branch — por encima del recomendado.

## W-VER-03 — Tests frágiles
✅ Sin hallazgos — cada test de `ArticulosControllerTests` usa su propia `CustomWebApplicationFactory` con SQLite in-memory de nombre único (`Guid.NewGuid()`), sin estado compartido ni dependencia de orden.

## Hallazgo adicional (no bloqueante)

Las ramas sad-path internas de `ValidateCodigo`/`ValidateDescripcion` en `ArticulosController.cs` (Código vacío/>30 caracteres; Descripción vacía/>200 caracteres) no tienen test dedicado — son las únicas líneas con `hits=0` del ticket. No viola F-VER-04 (el endpoint ya tiene sad-paths cubiertos) ni F-SPEC-16 (el spec no las lista como "Required tests" explícitamente). Recomendación no bloqueante: agregar 2 tests (`Alta con Código vacío → 400`, `Alta con Descripción > 200 caracteres → 400`) en una futura iteración.

## Trazabilidad PRD → Spec → Código → Tests

| FR | AC | Block | Código | Test |
|---|---|---|---|---|
| FR-01 | AC-01 | 1, 2, 3 | `Articulo.cs`, `ArticuloRepository.AddAsync`, `ArticulosController.Create` (Api/Web) | `Modulo5DbContextTests`, `Alta_de_articulo_con_datos_validos...` |
| FR-02 | AC-02 | 1, 2, 3 | `ArticuloRepository.DeleteAsync`, `ArticulosController.Delete` (Api/Web) | `Baja_de_articulo_existente...` |
| FR-03 | AC-03 | 1, 2, 3 | `ArticuloRepository.UpdateAsync`, `ArticulosController.Update` (Api/Web) | `Modificacion_de_articulo_existente...` |
| FR-04 | AC-04 | 1, 2 | `PrecioVentaCalculator.Calcular` | `PrecioVentaCalculatorTests`, `Alta_calcula_PrecioVenta...` |
| FR-05 | AC-05 | 1, 2 | `ArticuloRepository.SaveChangesTranslatingConstraintViolationsAsync` | `Persistir_dos_Articulo...`, `Alta_con_Codigo_duplicado...` |
| FR-06 | AC-06 | 1, 2 | `ArticuloValidationPolicy.Validate` | `ArticuloValidationPolicyTests` (Theory), `Alta_o_modificacion_con_valor_negativo...` |
| FR-07 | AC-07 | 1, 2 | `ArticuloValidationPolicy.Validate` | `ArticuloValidationPolicyTests` (3 Facts), `Alta_o_modificacion_que_rompe_orden...` |

## Resumen

**FAILs: 0 | WARNINGs: 3** (branch coverage en el piso exacto; verificación manual de Block 3 por inspección de código, no ejecución real; prosa desactualizada en "Final verification" del spec).

**Veredicto: PASSED.**
