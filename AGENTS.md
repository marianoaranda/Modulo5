# AGENTS.md — project context

> **DAW template.** Fill in the `[...]` with what is true of YOUR project and delete what does not
> apply. This file describes **the project**; **the process** is DAW's job (phases, gates, when to
> test, when to commit). Do not mix the two: process rules written here compete with the pipeline's.
>
> It is **tool-agnostic on purpose**: Claude Code reads it through the import in `CLAUDE.md`, Codex
> CLI, Copilot CLI, Cursor and OpenCode read it directly, and Gemini CLI gets it through
> `GEMINI.md`. The same file serves whichever tool you open the repo with — which is the point:
> porting the pipeline to another tool must not mean rewriting what your project is.

---

## Language

**Always respond in the language the user writes in.** Write every artifact you produce — PRDs,
specs, ADRs, reports, commit messages, status lines — in that same language, regardless of the
language these instructions are written in.

If this project has a fixed working language, state it here and use it instead:

> Working language: `[e.g. Spanish — write all artifacts in Spanish]`

---

## What this project is

[One or two sentences: what the app does and who for. Take it from the PRD, do not reinvent it.]

**Reference PRD:** `docs/daw/prd/[your-prd].md`

---

## Stack

**This is the only place the stack lives.** DAW reads it from here and generates no derived file.
Fill it in even if the repo is empty: without a stack there is nothing to plan or implement against.

If the repo already has code and this section is empty, DAW will detect the stack from your config
files and **propose the text for you to paste here**. You always confirm it.

| Field | Value |
|-------|-------|
| Language | C# (.NET 8) |
| Runtime | .NET 8 |
| Framework | ASP.NET MVC (Front-End) + ASP.NET Web API REST (Back-End), autenticación JWT |
| Database | SQL Server 2017 |
| Test runner | xUnit |
| Linter / formatter | dotnet format (EditorConfig) |
| Package manager | NuGet |

---

## Architecture conventions

**DAW validates your code against this section** during the CODE phase, via `daw-validate-arch`.
Leave it empty and that validation has nothing to compare against, so it stops being worth running.

- **Estructura de solución:** `Modulo5.sln` con `src/Modulo5.Web` (ASP.NET MVC, Front-End),
  `src/Modulo5.Api` (ASP.NET Web API REST, autenticación JWT), `src/Modulo5.Domain` (entidades y
  lógica de negocio), `src/Modulo5.Data` (EF Core 8 + SQL Server: DbContext, Migrations, seeds); y
  `tests/Modulo5.Domain.Tests`, `tests/Modulo5.Api.Tests` (xUnit).
- **Separación de capas:** `Web` nunca habla directo con `Data`; siempre a través de `Api`. `Api`
  expone controladores delgados que delegan las reglas de negocio a `Domain`; `Domain` no depende de
  `Data` ni de `Api` (solo define interfaces que `Data` implementa).
- **ORM:** Entity Framework Core 8, Code-First con Migrations. Las migraciones incluyen los seeds
  necesarios (p. ej. el perfil "administrador").
- **Autenticación:** JWT vía `Microsoft.AspNetCore.Authentication.JwtBearer`, expiración de 60
  minutos, sin refresh token. Contraseñas con PBKDF2 (`Rfc2898DeriveBytes`, nativo de .NET) y salt
  aleatorio de 16 bytes por usuario.
- **Manejo de errores:** excepciones de dominio tipadas (p. ej. `ValidationException`,
  `NotFoundException`) capturadas en un middleware único de la Web API que las traduce a códigos
  HTTP; nunca un catch silencioso.
- **Naming:** proyectos y clases en PascalCase (convención .NET estándar); archivos = nombre de la
  clase que contienen.
- **Dependencias:** ninguna librería nueva sin justificarla en el spec del ticket que la introduce.

---

## Code conventions

- [e.g. No `any`. If it is unavoidable, it comes with a comment explaining why.]
- [e.g. Pure functions wherever possible; side effects at the edges.]
- [e.g. Comments only when the *why* is not obvious from the code.]

---

## What NOT to do in this project

This section is worth its weight in gold: it is where the scars go, the things that already went
wrong once.

- [e.g. Do not touch `config/` without asking.]
- [e.g. Never call the payments API in tests — there is a mock.]
- [e.g. No destructive migrations.]

---

## Domain glossary

The terms specific to your product, so the agent uses them correctly instead of inventing synonyms.

- **[Term]:** [what it means exactly, here]
- **[Term]:** [what it means exactly, here]

---

> ℹ️ **What does NOT belong in this file, because DAW provides it:** the order work happens in, when
> the spec gets written, when tests run, when to commit, what it takes to move between phases. All
> of that lives in `.daw/` and applies on its own.

<!-- BEGIN DAW (managed by DAW — do not edit by hand) -->
# DAW — Dilux Agentic Workflow

This repo uses **DAW**: an agent-driven development pipeline with the phases
`CLASSIFY → DEFINE → PLAN → CODE → VERIFY → RELEASE`.

Before answering, read `.daw/orchestrator.md` and run its Boot Sequence. It is a strict state
machine: it decides what you are allowed to do based on the phase recorded in `.daw-state.json`.

The project's own context — stack, architecture, domain — is elsewhere in this file. It lives here,
in `AGENTS.md`, and not in any one tool's file, on purpose: it is tool-agnostic and comes along
unchanged when the pipeline is ported to another agent.
<!-- END DAW -->
