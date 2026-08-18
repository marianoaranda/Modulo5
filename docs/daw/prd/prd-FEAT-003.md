# PRD FEAT-003: Página Home y reestructuración de navegación web

| Field | Value |
|-------|-------|
| Ticket | FEAT-003 |
| Tracker | none |
| Date | 2026-08-18 |
| PRD loops | 1 |

## Context and Problem

Hoy `Modulo5.Web` no tiene una página de aterrizaje: tras el login, el usuario cae directo en
`/Usuarios`, y el menú de navegación (links a Usuarios y Articulos, botón Cerrar sesión) vive en
`_Layout.cshtml`, compartido por todas las vistas — incluidas las de Usuarios y Articulos, donde el
propio menú termina repitiendo el link a la sección en la que ya se está parado (FIX-001 lo hizo
más evidente al agregar el link a Articulos ahí también).

Se pide reestructurar la navegación: centralizar el menú en una nueva página Home, y que las
secciones de trabajo (Usuarios, Articulos) solo ofrezcan un camino de vuelta a Home, no el menú
completo.

## Goals

- Dar a la app un punto de entrada post-login claro y único (Home).
- Que el menú de navegación (Usuarios, Articulos, Cerrar sesión) exista en un solo lugar: Home.
- Que las secciones de trabajo (Usuarios, Articulos) no dupliquen el menú — solo un link de vuelta.

## Functional Requirements

- FR-01: El sistema debe tener una página Home (`GET /Home` o equivalente), accesible solo a
  usuarios autenticados. A diferencia de los `GET` actuales de `Usuarios`/`Articulos` (que no llaman
  a la Api y por lo tanto no validan la sesión), el `GET` de Home debe hacer una llamada autenticada
  a `Modulo5.Api` para forzar la validación del JWT en cada visita — mismo mecanismo de detección de
  401 que ya usan las acciones POST de ambos controllers, aplicado también acá.
- FR-02: La página Home debe mostrar: un link a Usuarios, un link a Articulos, y el control
  "Cerrar sesión" (mismo comportamiento que el botón actual de `_Layout.cshtml`).
- FR-03: Tras un login exitoso, el sistema debe redireccionar a Home (en vez de a `/Usuarios`, como
  hace hoy).
- FR-04: Las vistas de Usuarios (`Index`, `Create`, `Edit`) no deben mostrar el menú de navegación
  (links a Usuarios/Articulos ni el botón Cerrar sesión) — deben mostrar únicamente un link "Volver
  a Home".
- FR-05: Las vistas de Articulos (`Index`, `Create`, `Edit`) no deben mostrar el menú de navegación
  — deben mostrar únicamente un link "Volver a Home".

## Non-Functional Requirements

- NFR-01: La página Home reutiliza el mismo mecanismo de autenticación/autorización que ya existe
  en `UsuariosController`/`ArticulosController` (redirect a Login cuando la Api responde 401) — no
  introduce un tipo de chequeo nuevo (p. ej. no valida el JWT localmente en el Web). La única
  diferencia es que en Home ese mecanismo se dispara desde el propio `GET`, en vez de solo desde una
  acción `POST`, porque Home es la única pantalla donde "recibir un 401" debe pasar con solo entrar
  a la página (AC-05).

## Acceptance Criteria

- AC-01: WHEN un usuario envía credenciales válidas en Login, THE Web SHALL redireccionar a Home
  (FR-03).
- AC-02: WHEN un usuario autenticado visita Home, THE Web SHALL mostrar un link a Usuarios, un link
  a Articulos, y el control Cerrar sesión (FR-01, FR-02).
- AC-03: WHEN un usuario autenticado visita cualquier vista de Usuarios (Index/Create/Edit), THE Web
  SHALL mostrar únicamente un link "Volver a Home", sin el menú de navegación ni el botón Cerrar
  sesión (FR-04).
- AC-04: WHEN un usuario autenticado visita cualquier vista de Articulos (Index/Create/Edit), THE
  Web SHALL mostrar únicamente un link "Volver a Home", sin el menú de navegación ni el botón Cerrar
  sesión (FR-05).
- AC-05: IF un usuario no autenticado solicita Home directamente, THEN THE Web SHALL redireccionar a
  Login, mismo comportamiento que Usuarios/Articulos hoy (FR-01).
- AC-06: WHEN un usuario hace clic en "Cerrar sesión" desde Home, THE Web SHALL cerrar la sesión y
  redireccionar a Login, mismo comportamiento que el botón actual (FR-02).

## Out of Scope

- Las vistas `Views/Error/Index.cshtml` y `Views/Shared/AccesoDenegado.cshtml` no cambian: quedan
  fuera de este ticket (no son parte del flujo de trabajo normal Usuarios/Articulos).
- No se agrega ninguna funcionalidad nueva a Usuarios ni Articulos más allá de quitarles el menú.
- No hay cambios en la Api (`Modulo5.Api`) ni en el esquema de datos — es un cambio exclusivo de
  `Modulo5.Web`.
- No se agrega breadcrumb ni navegación jerárquica más allá del link "Volver a Home".

## Risks and Mitigations

- **Riesgo:** al quitar el botón "Cerrar sesión" de Usuarios/Articulos, un usuario que quiera cerrar
  sesión desde ahí tiene que volver a Home primero. **Mitigación:** es el comportamiento
  explícitamente pedido; el link "Volver a Home" es siempre un clic, no un obstáculo real.
- **Riesgo:** las vistas de Usuarios/Articulos comparten hoy `_Layout.cshtml` con el menú
  hardcodeado; sacar el menú de ahí sin romper el resto de la estructura común (título, mensajes de
  TempData) es una decisión de diseño para PLAN, no de este PRD.

## Dependencies

- Depende de FEAT-001a (Login, `AccountController`) y FEAT-001b (`ArticulosController`), ya
  mergeados a `main`.
- Depende de FIX-001 (link de navegación a Articulos), ya mergeado — este ticket reemplaza esa
  solución por la estructura definitiva pedida acá.
