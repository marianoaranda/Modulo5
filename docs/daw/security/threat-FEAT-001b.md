# Threat Model FEAT-001b: ABM de Artículos

| Field | Value |
|-------|-------|
| Ticket | FEAT-001b |
| Date | 2026-08-18 |
| Scope | Bloques 1 a 3 del spec (`docs/daw/specs/spec-FEAT-001b.md`): entidad Articulo (Domain/Data),
  endpoints `/api/articulos` (Api), pantalla Web del ABM |

## Componentes y límites de confianza (F-TM-02)

```
[Navegador]  --HTTPS-->  [Modulo5.Web (ArticulosController MVC)]  --HTTPS-->  [Modulo5.Api (ArticulosController)]  -->  [Modulo5.Domain (PrecioVentaCalculator, ArticuloValidationPolicy)]  -->  [Modulo5.Data (ArticuloRepository)]  --TLS-->  [SQL Server]
   (no confiable)              (server-side)                                    (server-side)                              (in-process)                                                        (server-side)                (infra)
```

Mismos límites de confianza que FEAT-001a (reutiliza su Api/Web/JWT sin cambios); no se introduce
ningún componente ni boundary nuevo, solo nuevas rutas dentro de un boundary ya identificado:

- **Boundary 1 — Navegador ↔ Modulo5.Web:** único punto de input no confiable directo del usuario
  para el ABM de Artículos. Requiere HTTPS y protección CSRF (ya provista por la infraestructura de
  FEAT-001a).
- **Boundary 2 — Modulo5.Web ↔ Modulo5.Api:** Web reenvía el JWT ya emitido por FEAT-001a; ningún
  cambio en cómo se obtiene o transporta.
- **Boundary 3 — Modulo5.Api ↔ Modulo5.Domain:** in-process. `ArticuloValidationPolicy` revalida sus
  invariantes (FR-06/FR-07) aunque el controller ya haya llamado a la política — defensa en
  profundidad, la política nunca confía en que el caller ya filtró los valores.
- **Boundary 4 — Modulo5.Data ↔ SQL Server:** sin cambios (credenciales fuera de código, TLS).

## Análisis STRIDE por componente (F-TM-01)

### Modulo5.Web (ArticulosController MVC)

| STRIDE | Amenaza | Mitigación |
|---|---|---|
| Spoofing | Reutiliza el JWT ya protegido por FEAT-001a — sin superficie nueva | Heredado (cookie `HttpOnly`+`Secure`+`SameSite=Strict`) |
| Tampering | CSRF sobre altas/bajas/modificaciones de Artículos vía formularios | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` en Create/Edit/Delete (Block 3) |
| Repudiation | No queda registro Web-side de qué usuario ejecutó qué acción sobre el catálogo | Cubierto por el logging del Block 2 (ver Api abajo) — Web no duplica esta responsabilidad |
| Information Disclosure | Página de error por defecto expone stack trace | Heredado de FEAT-001a (`UseExceptionHandler("/Error")`) — sin cambios |
| Denial of Service | No aplica de forma diferencial (NFR-04: 1-5 usuarios concurrentes) | — |
| Elevation of Privilege | N/A — a diferencia de Usuarios, este ABM no reserva la pantalla a un perfil (ver riesgo #1 abajo, a nivel Api) | — |

### Modulo5.Api (ArticulosController)

| STRIDE | Amenaza | Mitigación |
|---|---|---|
| Spoofing | Reutiliza la validación de JWT ya construida en FEAT-001a | Heredado — sin cambios |
| Tampering | Un cliente envía `precioVenta` directamente en el body, intentando fijar un precio de venta que no corresponde a `PrecioCosto × (1 + Margen/100)` | El controller SIEMPRE recalcula `PrecioVenta` con `PrecioVentaCalculator` server-side; cualquier `PrecioVenta` en el request se ignora (spec Block 2, "Logic") |
| Repudiation | Ninguna operación de ABM de Artículos queda asociada a qué usuario la ejecutó — a diferencia de Usuarios (FEAT-001a Block 4), el borrador de este spec no lo incluía | **Riesgo #3 abajo — mitigación agregada al Block 2 del spec**: loguear `UsuarioId` del actor (claim del JWT) + `Codigo` + operación + timestamp en cada alta/modificación/baja exitosa, mismo patrón que `UsuariosController.LogOperacionExitosa` |
| Information Disclosure | `ArticuloResponse` expone accidentalmente un campo interno futuro | DTO de respuesta explícito, mismo patrón que `UsuarioResponse` (Block 2) |
| Denial of Service | No aplica de forma diferencial (NFR-04, mismo criterio que Usuarios en FEAT-001a) | — |
| Elevation of Privilege | Cualquier usuario autenticado — sin importar su perfil — puede dar de alta, modificar o eliminar artículos del catálogo (precios, márgenes, umbrales de stock) | **Riesgo #1 abajo — accepted risk**, ver sección siguiente |

### Modulo5.Domain (PrecioVentaCalculator, ArticuloValidationPolicy, entidad Articulo)

| STRIDE | Amenaza | Mitigación |
|---|---|---|
| Spoofing / Tampering / Repudiation / DoS | N/A (in-process, sin input externo directo) | — |
| Information Disclosure | N/A — no maneja credenciales ni PII | — |
| Elevation of Privilege | Bug de cálculo que persista un `PrecioVenta` inconsistente con la fórmula (FR-04) | Tests explícitos de `PrecioVentaCalculator` (Block 1) cubriendo el caso base y el edge case Margen=0 |

### Modulo5.Data (ArticuloRepository, ArticuloConfiguration)

| STRIDE | Amenaza | Mitigación |
|---|---|---|
| Tampering | Inyección SQL vía `Codigo`/`Descripcion` | EF Core con LINQ parametrizado exclusivamente — ningún SQL crudo (heredado del patrón de `UsuarioRepository`) |
| Information Disclosure | N/A — `Articulo` no contiene credenciales ni PII | — |
| Repudiation / Spoofing / DoS / Elevation | N/A | — |

## Clasificación de datos sensibles (F-TM-05)

| Dato | Clasificación | Protección |
|---|---|---|
| PrecioCosto, Margen, PrecioVenta | Datos comerciales/financieros del negocio (no PII, no credenciales) | Accesible solo vía Api autenticada (JWT); en tránsito por TLS. No requiere cifrado en reposo adicional bajo F-TM-07 (esa regla exige cifrado para PII/credenciales específicamente — este dato no cae en esa categoría) |
| Codigo, Descripcion, StockMinimo, PuntoPedido, StockIdeal | Datos operativos del catálogo (no PII, no credenciales) | Igual que arriba |

No hay dato de esta feature clasificable como PII o credencial — F-TM-07 no aplica ningún requisito de cifrado adicional sobre lo ya heredado de FEAT-001a (TLS en tránsito en los 4 saltos).

## Riesgos identificados

| # | Riesgo | STRIDE | Severidad | Mitigación |
|---|---|---|---|---|
| 1 | Cualquier usuario autenticado, sin importar su perfil, puede alta/baja/modificar artículos (precios, márgenes, umbrales de stock) | Elevation of Privilege | 🟡 MEDIUM | **Accepted risk** — ver detalle abajo |
| 2 | Cliente envía `PrecioVenta` directamente intentando evitar la fórmula de FR-04 | Tampering | 🟠 HIGH | Ya incorporado al diseño: recálculo server-side siempre, ignora el valor del cliente (Block 2) |
| 3 | Ninguna operación del ABM de Artículos queda asociada a qué usuario la ejecutó | Repudiation | 🟡 MEDIUM | Mitigación agregada al Block 2: logging de actor + Código + operación + timestamp |
| 4 | Inyección SQL vía `Codigo`/`Descripcion` | Tampering | 🟢 LOW | EF Core parametrizado (heredado) |
| 5 | Fuerza bruta / degradación de servicio sobre `/api/articulos` | Denial of Service | 🟢 LOW | N/A bajo NFR-04 (1-5 usuarios concurrentes), mismo criterio que Usuarios en FEAT-001a |
| 6 | DTO de respuesta expone un campo interno no previsto | Information Disclosure | 🟢 LOW | `ArticuloResponse` explícito (Block 2) |

### Riesgo #1 — Accepted risk (F-TM-04)

El PRD de Artículos (`docs/daw/prd/prd-FEAT-001b.md`, FR-01 a FR-07) no exige ninguna restricción por
perfil de seguridad para el ABM de Artículos — a diferencia del PRD maestro, que sí la exige
explícitamente para Usuarios (RF-10: "La carga de usuarios... solo debe estar accesible para usuarios
del perfil administrador"). No hay un requisito equivalente para Artículos en ningún FR/RF. Diseñar
una restricción de perfil no pedida sería scope no aprobado (gold-plating).

- **Quién lo acepta:** Mariano Aranda (product owner del proyecto), confirmado explícitamente el
  2026-08-18 durante PLAN de FEAT-001b.
- **Justificación:** el PRD (FR-01 a FR-07) no exige restricción por perfil para el ABM de Artículos;
  agregar una sin que el PRD la pida sería una decisión de producto no solicitada.
- **Condiciones de revisión:** si el negocio decide en el futuro que el ABM de Artículos debe
  restringirse a un perfil (p. ej. "administrador" o uno nuevo como "encargado de catálogo"), la
  infraestructura ya existe (`AdminOnlyRequirement`/`AdminOnlyHandler` de FEAT-001a son reusables sin
  cambios) — solo requiere agregar `[Authorize(Policy = "AdminOnly")]` al controller y el PRD
  correspondiente.

## Mitigaciones a plegar en el spec

1. **Logging de auditoría (Riesgo #3):** agregado al Block 2 del spec — cada alta/modificación/baja
   exitosa loguea `UsuarioId` del actor (claim del JWT) + `Codigo` del artículo + operación +
   timestamp, mismo patrón que `UsuariosController.LogOperacionExitosa` de FEAT-001a.

## Dependencias externas (W-TM-01)

Ninguna dependencia NuGet nueva — reutiliza exactamente el stack ya declarado en `AGENTS.md` y ya
instalado por FEAT-001a. Sin riesgo de cadena de suministro nuevo.

## Resumen

Risks: C:0 H:1 M:2 L:3. El único HIGH (#2) ya está mitigado por diseño en el spec. El riesgo #1
(MEDIUM) se documenta como accepted risk pendiente de confirmación explícita del usuario (F-TM-04).
El riesgo #3 (MEDIUM) tiene mitigación plegada al Block 2 del spec.
