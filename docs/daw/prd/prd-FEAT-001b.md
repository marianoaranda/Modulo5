# PRD FEAT-001b: ABM de Artículos

| Field | Value |
|-------|-------|
| Ticket | FEAT-001b |
| Tracker | none |
| Date | 2026-08-17 |
| PRD loops | 0 |

## Context and Problem

Sub-ticket `b` del split de FEAT-001 (ver `docs/daw/prd/prd-FEAT-001.md`, ahora índice). En un
pequeño comercio de barrio, el dueño necesita poder mantener el catálogo de artículos que compra y
vende: alta, baja y modificación, con el cálculo automático del precio de venta y validaciones que
eviten datos inconsistentes (códigos duplicados, valores negativos, umbrales de stock fuera de
orden). Este ticket depende de FEAT-001a, que provee la autenticación JWT bajo la cual se exponen
estos endpoints.

## Goals

- Permitir el alta, baja y modificación de artículos del catálogo.
- Calcular automáticamente el Precio de Venta a partir del Precio de Costo y el Margen.
- Evitar que se persistan artículos con datos inconsistentes (código duplicado, valores negativos,
  umbrales de stock fuera de orden).

## Functional Requirements

**Artículos** (equivalen a RF-13 a RF-19 del PRD maestro)
- FR-01: El sistema debe permitir dar de alta un artículo con los campos Código, Descripción, Precio
  de Costo, Margen (%), Precio de Venta (calculado automáticamente según FR-04), Stock Mínimo, Punto
  de Pedido y Stock Ideal.
- FR-02: El sistema debe permitir dar de baja un artículo existente.
- FR-03: El sistema debe permitir modificar los datos de un artículo existente.
- FR-04: El sistema debe calcular automáticamente el Precio de Venta de cada artículo a partir del
  Precio de Costo y el Margen (%), aplicando la fórmula: Precio de Venta = Precio de Costo × (1 +
  Margen / 100).
- FR-05: El sistema debe rechazar el alta o la modificación de un artículo cuyo Código coincida con
  el de otro artículo ya existente, mostrando el mensaje "Ya existe un artículo con el Código
  ingresado." y sin grabar el registro.
- FR-06: El sistema debe rechazar el alta o la modificación de un artículo si alguno de los campos
  Precio de Costo, Margen, Stock Mínimo, Punto de Pedido o Stock Ideal es un valor negativo, mostrando
  el mensaje "Los valores de Precio de Costo, Margen, Stock Mínimo, Punto de Pedido y Stock Ideal no
  pueden ser negativos." y sin grabar el registro.
- FR-07: El sistema debe rechazar el alta o la modificación de un artículo que no cumpla la condición
  Stock Mínimo ≤ Punto de Pedido ≤ Stock Ideal, mostrando el mensaje "El Stock Mínimo debe ser menor
  o igual al Punto de Pedido, y este menor o igual al Stock Ideal." y sin grabar el registro.

## Non-Functional Requirements

- NFR-01: El Front-End debe ser un sitio Web ASP.NET MVC con .NET 8.
- NFR-02: El Back-End debe estar implementado completamente en una Web API REST con .NET 8, en un
  proyecto aparte, con autenticación JWT (provista por FEAT-001a), invocada desde el Front-End.
- NFR-03: La base de datos debe ser SQL Server 2017.
- NFR-04: El sistema debe soportar entre 1 y 5 usuarios concurrentes.

## Acceptance Criteria

**Artículos**
- AC-01: WHEN se da de alta un artículo con datos válidos, THE sistema SHALL persistirlo y permitir
  su recuperación por Código. (FR-01)
- AC-02: WHEN se elimina un artículo existente, THE sistema SHALL eliminarlo de forma que ya no
  pueda recuperarse por su Código. (FR-02)
- AC-03: WHEN se modifican los datos de un artículo existente, THE sistema SHALL persistir los
  cambios. (FR-03)
- AC-04: WHEN se graba un artículo con Precio de Costo y Margen (%) informados, THE sistema SHALL
  calcular el Precio de Venta como Precio de Costo × (1 + Margen / 100). (FR-04)
- AC-05: IF el Código de un artículo a dar de alta o modificar coincide con el de otro artículo ya
  existente, THEN THE sistema SHALL rechazar la operación, mostrar el mensaje "Ya existe un artículo
  con el Código ingresado." y no grabar el registro. (FR-05)
- AC-06: IF alguno de los campos Precio de Costo, Margen, Stock Mínimo, Punto de Pedido o Stock
  Ideal de un artículo es negativo, THEN THE sistema SHALL rechazar la operación, mostrar el mensaje
  "Los valores de Precio de Costo, Margen, Stock Mínimo, Punto de Pedido y Stock Ideal no pueden ser
  negativos." y no grabar el registro. (FR-06)
- AC-07: IF un artículo no cumple la condición Stock Mínimo ≤ Punto de Pedido ≤ Stock Ideal, THEN
  THE sistema SHALL rechazar la operación, mostrar el mensaje "El Stock Mínimo debe ser menor o
  igual al Punto de Pedido, y este menor o igual al Stock Ideal." y no grabar el registro. (FR-07)

## Out of Scope

- ABM de Perfiles de seguridad (RF-01 a RF-03 del PRD maestro).
- Autenticación, usuarios y login JWT (RF-04 a RF-12) — construidos en FEAT-001a, del cual depende
  este ticket.
- Popup reutilizable de Búsqueda de Artículos (RF-28 a RF-41), incluyendo el botón de lupa y la
  Descripción de solo lectura asociada al Código en la pantalla de ABM de Artículos.
- Movimientos de compra/venta (RF-20 a RF-24, RF-46 a RF-63).
- Consultas "Consulta de Stock Actual" y "Generar Pedido" (RF-25, RF-26, RF-42 a RF-45).
- Registro de errores en tabla de errores (RF-27).

## Risks and Mitigations

- Riesgo: que los endpoints de Artículos se implementen antes de que FEAT-001a esté disponible,
  quedando sin protección JWT. Mitigación: no iniciar CODE de este ticket hasta que FEAT-001a esté
  mergeado (verificación en PLAN/CLASSIFY del sub-ticket, según `.daw/rules/branches.instructions.md`
  §"Sub-tickets con dependencias").
- Dependencia: FEAT-001a (autenticación JWT).

## Dependencies

FEAT-001a — este ticket requiere que la autenticación JWT construida en FEAT-001a esté disponible
para proteger sus endpoints (NFR-02).
