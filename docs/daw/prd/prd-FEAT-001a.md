# PRD FEAT-001a: Autenticación (Usuarios, Credenciales y Login JWT)

| Field | Value |
|-------|-------|
| Ticket | FEAT-001a |
| Tracker | none |
| Date | 2026-08-17 |
| PRD loops | 0 |

## Context and Problem

Sub-ticket `a` del split de FEAT-001 (ver `docs/daw/prd/prd-FEAT-001.md`, ahora índice). Antes de
poder exponer cualquier funcionalidad del sistema — empezando por el catálogo de artículos — hace
falta que existan usuarios, que sus contraseñas se guarden de forma segura, y que el sistema exija
una sesión autenticada (JWT) para acceder a los endpoints protegidos. Este ticket construye esa base.

## Goals

- Permitir que un administrador gestione usuarios del sistema de forma segura (contraseñas
  hasheadas con salt).
- Permitir el inicio de sesión y proteger el resto del sistema con autenticación JWT.
- Contar con un perfil "administrador" disponible desde el primer arranque del sistema.

## Functional Requirements

**Usuarios** (equivalen a RF-04 a RF-06 del PRD maestro)
- FR-01: El sistema debe permitir dar de alta un usuario con los campos UsuarioId (autonumérico),
  Usuario, NombreCompleto, Hash y Salt.
- FR-02: El sistema debe permitir dar de baja un usuario existente.
- FR-03: El sistema debe permitir modificar los datos de un usuario existente.

**Seguridad de credenciales** (equivalen a RF-07 a RF-09)
- FR-04: La contraseña de cada usuario debe almacenarse como un hash generado a partir de dicha
  contraseña, de forma que no sea posible desencriptarla ni recuperarla en texto plano.
- FR-05: El hash de la contraseña de cada usuario debe generarse utilizando un salt aleatorio propio
  de ese usuario, de forma que dos usuarios con la misma contraseña tengan hashes distintos entre sí.
- FR-06: El sistema debe rechazar el alta o la modificación de un usuario cuya contraseña tenga
  menos de 8 caracteres alfanuméricos, mostrando el mensaje "La contraseña debe tener al menos 8
  caracteres alfanuméricos." y sin grabar el registro.

**Acceso** (equivalen a RF-10 a RF-12)
- FR-07: La carga de usuarios (FR-01, FR-02, FR-03) solo debe estar accesible para usuarios del
  perfil administrador.
- FR-08: El sistema debe tener una pantalla de inicio de sesión, donde se pida usuario y contraseña,
  validando la contraseña contra el hash asociado al usuario (teniendo en cuenta su salt) y emitiendo
  un token JWT cuando el ingreso es válido.
- FR-09: El sistema debe exigir un token JWT válido para acceder a cualquier endpoint protegido de
  la API, con excepción del endpoint de inicio de sesión. Toda solicitud sin un token JWT válido debe
  ser rechazada con error 401.

**Perfil administrador** (decisión de alcance de este ticket)
- FR-10: El perfil de seguridad "administrador" debe existir precargado en la base de datos mediante
  un script/migración de seed, sin requerir una pantalla de ABM de Perfiles (esa pantalla,
  correspondiente a RF-01 a RF-03 del PRD maestro, queda fuera de alcance — ver "Out of Scope").

## Non-Functional Requirements

- NFR-01: El Front-End debe ser un sitio Web ASP.NET MVC con .NET 8.
- NFR-02: El Back-End debe estar implementado completamente en una Web API REST con .NET 8, en un
  proyecto aparte, con autenticación JWT, invocada desde el Front-End.
- NFR-03: La base de datos debe ser SQL Server 2017.
- NFR-04: El sistema debe soportar entre 1 y 5 usuarios concurrentes.

## Acceptance Criteria

**Usuarios**
- AC-01: WHEN un administrador da de alta un usuario con datos válidos, THE sistema SHALL
  persistirlo y permitir su recuperación por UsuarioId. (FR-01)
- AC-02: WHEN un administrador elimina un usuario existente, THE sistema SHALL eliminarlo de forma
  que ya no pueda recuperarse por su UsuarioId. (FR-02)
- AC-03: WHEN un administrador modifica los datos de un usuario existente, THE sistema SHALL
  persistir los cambios. (FR-03)

**Seguridad de credenciales**
- AC-04: WHEN se da de alta un usuario con una contraseña, THE sistema SHALL almacenar dicha
  contraseña como un hash no reversible, nunca en texto plano ni en un formato recuperable. (FR-04)
- AC-05: WHEN se dan de alta dos usuarios con la misma contraseña, THE sistema SHALL generar un salt
  aleatorio distinto para cada uno, de forma que sus hashes resultantes difieran entre sí. (FR-05)
- AC-06: IF la contraseña informada en el alta o modificación de un usuario tiene menos de 8
  caracteres alfanuméricos, THEN THE sistema SHALL rechazar la operación, mostrar el mensaje "La
  contraseña debe tener al menos 8 caracteres alfanuméricos." y no grabar el registro. (FR-06)
- AC-07: WHEN la contraseña informada tiene 8 o más caracteres alfanuméricos, THE sistema SHALL
  aceptar la operación y persistir el registro. (FR-06)

**Acceso**
- AC-08: IF un usuario cuyo perfil no es administrador intenta acceder a la funcionalidad de alta,
  baja o modificación de usuarios, THEN THE sistema SHALL denegar el acceso. (FR-07)
- AC-09: IF se intenta iniciar sesión con un usuario inexistente o con una contraseña incorrecta,
  THEN THE sistema SHALL mostrar el mensaje "Usuario o contraseña incorrectos" y no autorizar el
  ingreso. (FR-08)
- AC-10: WHEN un usuario existente inicia sesión con la contraseña correcta, THE sistema SHALL
  autorizar el ingreso y emitir un token JWT. (FR-08)
- AC-11: IF una solicitud a un endpoint protegido de la API no incluye un token JWT válido, THEN THE
  sistema SHALL responder con error 401 (No autorizado) y denegar el acceso. (FR-09)

**Perfil administrador**
- AC-12: WHEN se ejecuta el seed inicial de la base de datos, THE sistema SHALL crear el perfil de
  seguridad "administrador" precargado, disponible para el login y el control de acceso sin
  necesidad de una pantalla de ABM de Perfiles. (FR-10)

## Out of Scope

- ABM de Perfiles de seguridad (alta, baja, modificación de perfiles — RF-01 a RF-03 del PRD
  maestro). El perfil "administrador" existe únicamente como dato precargado (FR-10).
- ABM de Artículos (RF-13 a RF-19 del PRD maestro) — es el sub-ticket FEAT-001b.
- Popup reutilizable de Búsqueda de Artículos (RF-28 a RF-41).
- Movimientos de compra/venta (RF-20 a RF-24, RF-46 a RF-63).
- Consultas "Consulta de Stock Actual" y "Generar Pedido" (RF-25, RF-26, RF-42 a RF-45).
- Registro de errores en tabla de errores (RF-27).
- Refresh tokens o mecanismos de renovación de sesión: el detalle de expiración y renovación del JWT
  se decide en la fase de PLAN.

## Risks and Mitigations

- Riesgo: que el algoritmo de hashing/salt elegido en PLAN no sea el adecuado para las 5 concurrencias
  esperadas (NFR-04). Mitigación: usar un algoritmo estándar de la industria (a definir en PLAN, p.
  ej. PBKDF2 o BCrypt) con costo configurable.
- Dependencia: ninguna.

## Dependencies

Ninguna — es el primer sub-ticket del split de FEAT-001. FEAT-001b (ABM de Artículos) depende de
este ticket: sus endpoints necesitan el mecanismo de autenticación JWT construido acá.
