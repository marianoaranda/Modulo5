# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/).

## [Unreleased]

## [0.1.0] - 2026-08-17

### Added

- [FEAT-001a] Autenticación: alta/baja/modificación de usuarios (ABM), hashing de contraseñas con PBKDF2 + salt aleatorio, política de contraseñas, login con JWT (expiración de 60 minutos, sin refresh token), autorización `AdminOnly` para el ABM de Usuarios, perfil "administrador" sembrado por migración, y pantalla web MVC de login y ABM de Usuarios.
