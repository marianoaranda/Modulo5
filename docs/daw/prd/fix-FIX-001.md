# Fix FIX-001: Agregar link de navegación al ABM de Artículos

- **Bug**: `src/Modulo5.Web/Views/Shared/_Layout.cshtml` solo tiene un link a `/Usuarios` en el
  `<nav>`. El ABM de Artículos (`/Articulos`, agregado en FEAT-001b) existe y funciona, pero no es
  alcanzable desde ninguna vista de la app — solo escribiendo la URL a mano.
- **Change**: `src/Modulo5.Web/Views/Shared/_Layout.cshtml:11` (dentro del `<nav>`) — agregar
  `<a href="/Articulos">Artículos</a>` junto al link existente de `/Usuarios`.
- **Regression test**: no hay `Modulo5.Web.Tests` (mismo criterio que FEAT-001a/FEAT-001b, ver
  AGENTS.md "Stack"). Verificación manual en CODE: logueado como `admin`, el `<nav>` muestra ambos
  links y `/Articulos` navega al panel del ABM de Artículos.
- **Risk**: none — solo agrega un `<a>` a una vista compartida; no toca lógica, endpoints ni
  autorización.
