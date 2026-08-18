# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/).

## [Unreleased]

### Added

- [FEAT-001b] ABM de Artículos: alta/baja/modificación de artículos (Código, Descripción, Precio de Costo, Margen, Stock Mínimo, Punto de Pedido, Stock Ideal), cálculo automático del Precio de Venta (Precio de Costo × (1 + Margen/100)) siempre recalculado server-side, validaciones de negocio (Código único, sin valores negativos, Stock Mínimo ≤ Punto de Pedido ≤ Stock Ideal), endpoints REST protegidos por JWT (sin restricción de perfil), auditoría de operaciones (actor + Código + operación + timestamp), y pantalla web MVC del ABM.

## [0.1.0] - 2026-08-17

### Added

- [FEAT-001a] Autenticación: alta/baja/modificación de usuarios (ABM), hashing de contraseñas con PBKDF2 + salt aleatorio, política de contraseñas, login con JWT (expiración de 60 minutos, sin refresh token), autorización `AdminOnly` para el ABM de Usuarios, perfil "administrador" sembrado por migración, y pantalla web MVC de login y ABM de Usuarios.
