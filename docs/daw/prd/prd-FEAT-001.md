# Parent PRD: Carga de Artículos

| Metric | Value |
|--------|-------|
| Ticket | FEAT-001 |
| Date | 2026-08-17 |
| Status | Split |

## Sub-tickets

| Sub-ticket | Title | PRD | Dependencies | Status |
|---|---|---|---|---|
| FEAT-001a | Autenticación (Usuarios, Credenciales y Login JWT) | prd-FEAT-001a.md | none | active |
| FEAT-001b | ABM de Artículos | prd-FEAT-001b.md | depends on FEAT-001a | pending |

## Suggested implementation order
FEAT-001a → FEAT-001b

## Original context

Este ticket nació como "Carga de Artículos" (ABM de Artículos, RF-13 a RF-19 del PRD maestro
`docs/daw/prd/PRD.md`). Al redactar el PRD se detectó que esa pantalla depende de que exista
autenticación (usuarios, contraseñas hasheadas, login, JWT — RF-04 a RF-12), que todavía no existe
en el proyecto por ser su primer ticket. El PRD resultante tenía 19 ACs sobre 4 áreas distintas
(Usuarios, Seguridad de credenciales, Acceso/Login, Artículos), por encima de la guía de 5-7 ACs de
DAW, y se dividió en dos sub-tickets independientemente entregables: FEAT-001a (la base de
autenticación) y FEAT-001b (el ABM de Artículos en sí, que depende de FEAT-001a).
